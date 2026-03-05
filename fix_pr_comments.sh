#!/bin/bash
# Fix 1: Add aria-label and title to Trash2 button in AiModelsTable.tsx
sed -i 's/<Button/<Button\n                          aria-label={`Delete ${config.feature} configuration`}\n                          title="Delete Configuration"/g' apps/admin_page/src/components/ai-models/AiModelsTable.tsx
# Clean up duplicate <Button tags if sed matched too many
sed -i '/<Button/!b;n;/aria-label=/!b;n;/title=/!b' apps/admin_page/src/components/ai-models/AiModelsTable.tsx # This is tricky with sed, let's use python or diff instead
