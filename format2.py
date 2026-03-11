import re
import sys

def format_file(file_path):
    with open(file_path, 'r') as f:
        content = f.read()

    # Find the problematic catch block
    pattern = r'(\n[ \t]*)catch \(OperationCanceledException\) when \((cancellationToken|ct)\.IsCancellationRequested\)\n[ \t]*\{\n[ \t]*throw;\n[ \t]*\}'

    def repl(m):
        spaces = m.group(1)
        var_name = m.group(2)
        return f'{spaces}catch (OperationCanceledException) when ({var_name}.IsCancellationRequested){spaces}{{\n{spaces}    throw;{spaces}}}'

    new_content = re.sub(pattern, repl, content)

    with open(file_path, 'w') as f:
        f.write(new_content)

if __name__ == '__main__':
    for file in sys.argv[1:]:
        format_file(file)
