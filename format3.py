import sys
import re

def format_file(filepath):
    with open(filepath, 'r') as f:
        content = f.read()

    # Find the pattern and replace it properly indented
    pattern = re.compile(r'(\n[ \t]*)catch \(OperationCanceledException\) when \((cancellationToken|ct)\.IsCancellationRequested\)\s*\{\s*throw;\s*\}')

    def replacer(match):
        indent = match.group(1)
        var = match.group(2)
        return f'{indent}catch (OperationCanceledException) when ({var}.IsCancellationRequested){indent}{{{indent}    throw;{indent}}}'

    new_content = pattern.sub(replacer, content)

    with open(filepath, 'w') as f:
        f.write(new_content)

for filepath in sys.argv[1:]:
    format_file(filepath)
