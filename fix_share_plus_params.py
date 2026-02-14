import sys

with open('apps/flutter_app/lib/screens/context_report_screen.dart', 'r') as f:
    content = f.read()

# Fix SharePlus usage
content = content.replace(
    'SharePlus.instance.share(text, subject: \'Property Report: ${report.location.displayAddress}\');',
    'SharePlus.instance.share(ShareParams(text: text, subject: \'Property Report: ${report.location.displayAddress}\'));'
)

with open('apps/flutter_app/lib/screens/context_report_screen.dart', 'w') as f:
    f.write(content)
