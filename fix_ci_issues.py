import re

# Fix ContextReportScreen
with open('apps/flutter_app/lib/screens/context_report_screen.dart', 'r') as f:
    content = f.read()

# Fix Share deprecation
content = content.replace(
    'Share.share(text, subject:',
    'SharePlus.instance.share(text, subject:'
)

# Fix unused key parameter in _InputForm
content = content.replace(
    '  const _InputForm({\n    super.key,\n    required this.controller,',
    '  const _InputForm({\n    required this.controller,'
)

with open('apps/flutter_app/lib/screens/context_report_screen.dart', 'w') as f:
    f.write(content)

# Fix MapLegend unused imports
with open('apps/flutter_app/lib/widgets/insights/map_legend.dart', 'r') as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    if "import '../../core/theme/valora_colors.dart';" in line:
        continue
    if "import '../../core/theme/valora_spacing.dart';" in line:
        continue
    new_lines.append(line)

with open('apps/flutter_app/lib/widgets/insights/map_legend.dart', 'w') as f:
    f.writelines(new_lines)
