#!/bin/bash
# Pre-commit checklist
echo "Running pre-commit checks..."
npm run lint
npm test
echo "Pre-commit checks complete."
