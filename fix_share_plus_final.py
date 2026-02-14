with open('apps/flutter_app/lib/screens/context_report_screen.dart', 'r') as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if 'SharePlus.share' in line:
        # text is defined in the block above
        lines[i] = "    SharePlus.instance.share(ShareParams(text: text, subject: 'Property Report: ${report.location.displayAddress}'));\n"

with open('apps/flutter_app/lib/screens/context_report_screen.dart', 'w') as f:
    f.writelines(lines)
