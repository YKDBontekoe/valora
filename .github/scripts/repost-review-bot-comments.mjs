import fs from 'node:fs';

const CODE_RABBIT_LOGIN = 'coderabbitai';
const QODO_LOGIN = 'qodo-free-for-open-source-projects';
const REPOST_MARKER_VERSION = 'v1';
const SUPPORTED_BOTS = new Set([CODE_RABBIT_LOGIN, QODO_LOGIN]);
const REQUEST_TIMEOUT_MS = 30_000;

function logEvent(level, event, details = {}) {
  console.log(JSON.stringify({ level, event, ...details }));
}

function normalizeLogin(login) {
  if (!login) {
    return '';
  }

  return login.replace(/\[bot\]$/i, '').toLowerCase();
}

function stripQuotePrefixes(text) {
  return text
    .split('\n')
    .map((line) => line.replace(/^\s*>\s?/, ''))
    .join('\n');
}

export function parseCodeRabbitActionable(body) {
  const summaryMatch = body.match(
    /<summary>\s*🤖\s*Fix all issues with AI agents\s*<\/summary>[\s\S]*?```([\s\S]*?)```/i,
  );

  if (summaryMatch && summaryMatch[1]) {
    const actionableFromSummary = summaryMatch[1].trim();
    if (actionableFromSummary.length > 0) {
      return actionableFromSummary;
    }
  }

  const inlinePromptMatch = body.match(
    /<summary>\s*🤖\s*Prompt for AI Agents\s*<\/summary>[\s\S]*?```([\s\S]*?)```/i,
  );

  if (!inlinePromptMatch || !inlinePromptMatch[1]) {
    return null;
  }

  const actionableFromInlinePrompt = inlinePromptMatch[1].trim();
  return actionableFromInlinePrompt.length > 0 ? actionableFromInlinePrompt : null;
}

export function parseQodoIssues(body) {
  const isQodoBody =
    body.includes('<h3>Code Review by Qodo</h3>') ||
    body.includes('<summary><strong>Agent Prompt</strong></summary>');
  if (!isQodoBody) {
    return [];
  }

  const normalized = stripQuotePrefixes(body);
  const issueRegex =
    /<summary>\s*(?:✅\s*<s>\s*)?\d+\.\s*([^<\n]+?)\s*<code>\s*(?:🐞\s*Bugs?|📘\s*Rule\s*violations?|📎\s*Requirement\s*gaps?)\s*<\/code>[\s\S]*?<\/summary>/gi;

  const issues = [];
  const matches = [];
  let issueMatch;

  while ((issueMatch = issueRegex.exec(normalized)) !== null) {
    matches.push({
      title: issueMatch[1].replace(/<[^>]+>/g, '').trim(),
      index: issueMatch.index,
      length: issueMatch[0].length,
    });
  }

  for (let i = 0; i < matches.length; i += 1) {
    const current = matches[i];
    const next = matches[i + 1];
    const start = current.index + current.length;
    const end = next ? next.index : normalized.length;
    const section = normalized.slice(start, end);

    const promptMatch = section.match(
      /<summary>\s*Agent prompt\s*<\/summary>[\s\S]*?```([\s\S]*?)```/i,
    );

    if (promptMatch && promptMatch[1]) {
      const prompt = promptMatch[1].trim();
      if (prompt.length > 0) {
        issues.push({
          title: current.title,
          prompt,
        });
      }
    }
  }

  if (issues.length > 0) {
    return issues;
  }

  const inlinePromptMatch = normalized.match(
    /<summary>\s*<strong>\s*Agent Prompt\s*<\/strong>\s*<\/summary>[\s\S]*?```([\s\S]*?)```/i,
  );
  const inlineTitleMatch = normalized.match(
    /(?:^|\n)\s*\d+\\?\.\s*([^<\n]+?)\s*<code>\s*(?:🐞\s*Bugs?|📘\s*Rule\s*violations?|📎\s*Requirement\s*gaps?)\s*<\/code>/i,
  );

  if (inlinePromptMatch && inlinePromptMatch[1]) {
    issues.push({
      title: inlineTitleMatch?.[1]?.trim() || 'Qodo review issue',
      prompt: inlinePromptMatch[1].trim(),
    });
  }

  return issues;
}

export function buildRepostBody({ sourceLogin, sourceCommentId, sourceUrl, sourceBody }) {
  const marker = `<!-- reposted-from-bot-comment:${sourceCommentId}:${REPOST_MARKER_VERSION} -->`;
  const normalizedSourceLogin = normalizeLogin(sourceLogin);

  if (normalizedSourceLogin === CODE_RABBIT_LOGIN) {
    const actionable = parseCodeRabbitActionable(sourceBody);
    if (!actionable) {
      return null;
    }

    return [
      marker,
      `### Reposted from @${sourceLogin}`,
      `Source: ${sourceUrl}`,
      '',
      '```text',
      actionable,
      '```',
    ].join('\n');
  }

  if (normalizedSourceLogin === QODO_LOGIN) {
    const issues = parseQodoIssues(sourceBody);
    if (issues.length === 0) {
      return null;
    }

    const issueBlocks = issues
      .map(
        (issue, idx) =>
          `${idx + 1}. ${issue.title}\n\n\`\`\`text\n${issue.prompt}\n\`\`\``,
      )
      .join('\n\n');

    return [
      marker,
      `### Reposted from @${sourceLogin}`,
      `Source: ${sourceUrl}`,
      '',
      issueBlocks,
    ].join('\n');
  }

  return null;
}

function getRequiredEnv(name) {
  const value = process.env[name];
  if (!value) {
    throw new Error(`${name} is required`);
  }
  return value;
}

async function githubRequest({ token, method, path, body }) {
  const response = await fetch(`https://api.github.com${path}`, {
    method,
    headers: {
      Accept: 'application/vnd.github+json',
      Authorization: `Bearer ${token}`,
      'X-GitHub-Api-Version': '2022-11-28',
      'Content-Type': 'application/json',
    },
    body: body ? JSON.stringify(body) : undefined,
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`GitHub API ${method} ${path} failed (${response.status}): ${text}`);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

async function getAllIssueComments({ token, owner, repo, issueNumber }) {
  const comments = [];
  let page = 1;
  const maxPages = 10;

  while (page <= maxPages) {
    const pageComments = await githubRequest({
      token,
      method: 'GET',
      path: `/repos/${owner}/${repo}/issues/${issueNumber}/comments?per_page=100&page=${page}`,
    });

    comments.push(...pageComments);

    if (pageComments.length < 100) {
      break;
    }

    page += 1;
  }

  if (page > maxPages) {
    logEvent('warn', 'comments-pagination-capped', { issueNumber, maxPages });
  }

  return comments;
}

export function extractSourceFromPayload(payload) {
  if (!payload || typeof payload !== 'object') {
    return null;
  }

  if (payload.comment && payload.issue) {
    return {
      sourceType: 'issue_comment',
      sourceId: payload.comment.id,
      sourceLogin: payload.comment.user?.login ?? '',
      sourceUrl: payload.comment.html_url ?? '',
      sourceBody: payload.comment.body ?? '',
      issueNumber: payload.issue.number,
      isPullRequest: Boolean(payload.issue.pull_request),
    };
  }

  if (payload.comment && payload.pull_request) {
    return {
      sourceType: 'pull_request_review_comment',
      sourceId: payload.comment.id,
      sourceLogin: payload.comment.user?.login ?? '',
      sourceUrl: payload.comment.html_url ?? '',
      sourceBody: payload.comment.body ?? '',
      issueNumber: payload.pull_request.number,
      isPullRequest: true,
    };
  }

  if (payload.review && payload.pull_request) {
    return {
      sourceType: 'pull_request_review',
      sourceId: payload.review.id,
      sourceLogin: payload.review.user?.login ?? '',
      sourceUrl: payload.review.html_url ?? '',
      sourceBody: payload.review.body ?? '',
      issueNumber: payload.pull_request.number,
      isPullRequest: true,
    };
  }

  return null;
}

async function main() {
  const eventPath = getRequiredEnv('GITHUB_EVENT_PATH');
  const token = getRequiredEnv('VALORA_BOT_PAT');
  const repository = getRequiredEnv('GITHUB_REPOSITORY');

  const [owner, repo] = repository.split('/');
  const payload = JSON.parse(fs.readFileSync(eventPath, 'utf8'));
  const source = extractSourceFromPayload(payload);
  if (!source) {
    logEvent('info', 'skip-unsupported-webhook-payload');
    return;
  }

  const normalizedSourceLogin = normalizeLogin(source.sourceLogin);
  if (!SUPPORTED_BOTS.has(normalizedSourceLogin)) {
    logEvent('info', 'skip-unsupported-source-login', { sourceLogin: source.sourceLogin || 'unknown' });
    return;
  }

  if (!source.issueNumber || !source.isPullRequest) {
    logEvent('info', 'skip-not-pull-request-comment');
    return;
  }

  const marker = `<!-- reposted-from-bot-comment:${source.sourceId}:${REPOST_MARKER_VERSION} -->`;

  const existingComments = await getAllIssueComments({
    token,
    owner,
    repo,
    issueNumber: source.issueNumber,
  });
  const alreadyReposted = existingComments.some((comment) => comment.body?.includes(marker));

  if (alreadyReposted) {
    logEvent('info', 'skip-already-reposted', { sourceId: source.sourceId });
    return;
  }

  const repostBody = buildRepostBody({
    sourceLogin: source.sourceLogin,
    sourceCommentId: source.sourceId,
    sourceUrl: source.sourceUrl,
    sourceBody: source.sourceBody,
  });

  if (!repostBody) {
    logEvent('info', 'skip-no-actionable-content', { sourceId: source.sourceId });
    return;
  }

  await githubRequest({
    token,
    method: 'POST',
    path: `/repos/${owner}/${repo}/issues/${source.issueNumber}/comments`,
    body: { body: repostBody },
  });

  logEvent('info', 'repost-created', {
    sourceLogin: source.sourceLogin,
    issueNumber: source.issueNumber,
    sourceId: source.sourceId,
  });
}

if (process.env.GITHUB_EVENT_PATH) {
  main().catch((error) => {
    logEvent('error', 'repost-failed', {
      message: error?.message ?? String(error),
    });
    process.exit(1);
  });
}
