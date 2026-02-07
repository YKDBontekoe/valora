import test from 'node:test';
import assert from 'node:assert/strict';

import {
  buildRepostBody,
  extractSourceFromPayload,
  parseCodeRabbitActionable,
  parseQodoIssues,
} from './repost-review-bot-comments.mjs';

test('parseCodeRabbitActionable extracts AI-agent actionable block', () => {
  const body =
    '**Actionable comments posted: 2**\n\n' +
    '<details>\n' +
    '<summary>🤖 Fix all issues with AI agents</summary>\n\n' +
    '```\n' +
    'In @apps/flutter_app/lib/screens/search_screen.dart:\n' +
    '- Around line 425-430 ...\n' +
    '```\n\n' +
    '</details>';

  const parsed = parseCodeRabbitActionable(body);
  assert.equal(parsed, 'In @apps/flutter_app/lib/screens/search_screen.dart:\n- Around line 425-430 ...');
});

test('parseQodoIssues extracts issue title and agent prompt blocks', () => {
  const body =
    '<h3>Code Review by Qodo</h3>\n' +
    '<details>\n' +
    '<summary>✅ <s>  1.  Wrong navigator dismissed <code>🐞 Bug</code> <code>✓ Correctness</code></s></summary>\n\n' +
    '> <details>\n' +
    '><summary>Agent prompt</summary>\n' +
    '><br/>\n' +
    '>\n' +
    '>```\n' +
    '>The issue below was found during a code review.\n' +
    '>### Fix Focus Areas\n' +
    '>- apps/flutter_app/lib/screens/search_screen.dart[230-291]\n' +
    '>```\n' +
    '></details>\n\n' +
    '<hr/>\n' +
    '</details>';

  const issues = parseQodoIssues(body);
  assert.equal(issues.length, 1);
  assert.equal(issues[0].title, 'Wrong navigator dismissed');
  assert.match(issues[0].prompt, /Fix Focus Areas/);
});

test('buildRepostBody handles bot login with [bot] suffix', () => {
  const body =
    '**Actionable comments posted: 1**\n\n' +
    '<details>\n' +
    '<summary>🤖 Fix all issues with AI agents</summary>\n\n' +
    '```\n' +
    'Fix the issue\n' +
    '```\n\n' +
    '</details>';

  const repost = buildRepostBody({
    sourceLogin: 'coderabbitai[bot]',
    sourceCommentId: 123,
    sourceUrl: 'https://example.com',
    sourceBody: body,
  });

  assert.ok(repost);
  assert.match(repost, /Reposted from @coderabbitai\[bot\]/);
});

test('parseCodeRabbitActionable extracts inline Prompt for AI Agents block', () => {
  const body =
    '_⚠️ Potential issue_ | _🟡 Minor_\n\n' +
    '<details>\n' +
    '<summary>🤖 Prompt for AI Agents</summary>\n\n' +
    '```\n' +
    'In `@verification/verify_search.py` around lines 14 - 18...\n' +
    '```\n\n' +
    '</details>';

  const parsed = parseCodeRabbitActionable(body);
  assert.match(parsed ?? '', /verify_search.py/);
});

test('parseQodoIssues extracts inline review-comment Agent Prompt block', () => {
  const body =
    '<img src="https://www.qodo.ai/wp-content/uploads/2025/12/v2-action-required.svg" height="20" alt="Action required">\n\n' +
    '2\\. Sort ignored when empty <code>🐞 Bug</code> <code>✓ Correctness</code>\n\n' +
    '<details>\n' +
    '<summary><strong>Agent Prompt</strong></summary>\n\n' +
    '```\n' +
    '### Issue description\n' +
    'Selecting a sort option triggers `_loadListings(refresh: true)`...\n' +
    '### Fix Focus Areas\n' +
    '- apps/flutter_app/lib/screens/search_screen.dart[91-111]\n' +
    '```\n\n' +
    '</details>';

  const issues = parseQodoIssues(body);
  assert.equal(issues.length, 1);
  assert.equal(issues[0].title, 'Sort ignored when empty');
  assert.match(issues[0].prompt, /Fix Focus Areas/);
});

test('extractSourceFromPayload handles pull_request_review payload', () => {
  const payload = {
    review: {
      id: 55,
      body: 'review body',
      html_url: 'https://github.com/x/y/pull/1#pullrequestreview-55',
      user: {
        login: 'qodo-free-for-open-source-projects[bot]',
      },
    },
    pull_request: {
      number: 150,
    },
  };

  const source = extractSourceFromPayload(payload);
  assert.equal(source?.sourceType, 'pull_request_review');
  assert.equal(source?.sourceId, 55);
  assert.equal(source?.issueNumber, 150);
  assert.equal(source?.isPullRequest, true);
});

test('extractSourceFromPayload handles issue_comment payload on PR', () => {
  const payload = {
    comment: {
      id: 101,
      body: 'comment body',
      html_url: 'https://github.com/x/y/pull/1#issuecomment-101',
      user: { login: 'coderabbitai[bot]' },
    },
    issue: {
      number: 150,
      pull_request: { url: 'https://api.github.com/repos/x/y/pulls/150' },
    },
  };

  const source = extractSourceFromPayload(payload);
  assert.equal(source?.sourceType, 'issue_comment');
  assert.equal(source?.sourceId, 101);
  assert.equal(source?.issueNumber, 150);
  assert.equal(source?.isPullRequest, true);
});

test('extractSourceFromPayload handles pull_request_review_comment payload', () => {
  const payload = {
    comment: {
      id: 202,
      body: 'review comment body',
      html_url: 'https://github.com/x/y/pull/1#discussion_r202',
      user: { login: 'qodo-free-for-open-source-projects[bot]' },
    },
    pull_request: {
      number: 151,
    },
  };

  const source = extractSourceFromPayload(payload);
  assert.equal(source?.sourceType, 'pull_request_review_comment');
  assert.equal(source?.sourceId, 202);
  assert.equal(source?.issueNumber, 151);
  assert.equal(source?.isPullRequest, true);
});

test('buildRepostBody returns null when no actionable content exists', () => {
  const repost = buildRepostBody({
    sourceLogin: 'coderabbitai[bot]',
    sourceCommentId: 404,
    sourceUrl: 'https://example.com',
    sourceBody: 'No actionable block here.',
  });

  assert.equal(repost, null);
});

test('buildRepostBody handles qodo actionable content', () => {
  const body =
    '<h3>Code Review by Qodo</h3>\n' +
    '<details>\n' +
    '<summary>1. Missing validation <code>🐞 Bug</code> <code>✓ Correctness</code></summary>\n\n' +
    '<details>\n' +
    '<summary>Agent prompt</summary>\n\n' +
    '```\n' +
    'Add request validation in handler.\n' +
    '```\n\n' +
    '</details>\n' +
    '</details>';

  const repost = buildRepostBody({
    sourceLogin: 'qodo-free-for-open-source-projects[bot]',
    sourceCommentId: 505,
    sourceUrl: 'https://example.com/qodo',
    sourceBody: body,
  });

  assert.ok(repost);
  assert.match(repost, /Missing validation/);
  assert.match(repost, /Add request validation/);
});
