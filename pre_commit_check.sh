#!/bin/bash
set -e

# Get the root directory of the repository
REPO_ROOT=$(git rev-parse --show-toplevel)
echo "Running pre-commit checks in $REPO_ROOT..."

# 1. Backend: Run unit tests
echo "----------------------------------------"
echo "Running Backend Tests..."
dotnet test "$REPO_ROOT/backend/Valora.UnitTests" --configuration Release

# 2. Frontend: Flutter Analysis
echo "----------------------------------------"
echo "Running Frontend Analysis..."
cd "$REPO_ROOT/apps/flutter_app"
flutter analyze

# 3. Frontend: Flutter Unit Tests
echo "----------------------------------------"
echo "Running Frontend Tests..."
flutter test

echo "----------------------------------------"
echo "✅ Pre-commit checks complete. Proceeding with commit."