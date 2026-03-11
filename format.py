import re
import sys

def format_file(file_path):
    with open(file_path, 'r') as f:
        content = f.read()

    # Find the problematic catch block
    pattern = r'( +)catch \(OperationCanceledException\) when \(cancellationToken\.IsCancellationRequested\)\n\s+{\n\s+throw;\n\s+}'

    def repl(m):
        spaces = m.group(1)
        return f'{spaces}catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n{spaces}{{\n{spaces}    throw;\n{spaces}}}'

    new_content = re.sub(pattern, repl, content)

    with open(file_path, 'w') as f:
        f.write(new_content)

if __name__ == '__main__':
    for file in sys.argv[1:]:
        format_file(file)
