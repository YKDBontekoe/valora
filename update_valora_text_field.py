import sys

with open('apps/flutter_app/lib/widgets/common/valora_text_field.dart', 'r') as f:
    content = f.read()

# Add suffixIcon to constructor and class
content = content.replace(
    'final IconData? prefixIcon;',
    'final IconData? prefixIcon;\n  final Widget? suffixIcon;'
)

content = content.replace(
    'this.prefixIcon,',
    'this.prefixIcon,\n    this.suffixIcon,'
)

# Add suffixIcon to InputDecoration
content = content.replace(
    'prefixStyle: ValoraTypography.bodyMedium,',
    'prefixStyle: ValoraTypography.bodyMedium,\n            suffixIcon: widget.suffixIcon,'
)

with open('apps/flutter_app/lib/widgets/common/valora_text_field.dart', 'w') as f:
    f.write(content)
