#!/bin/bash
set -e
echo "Running pre-commit checks..."

# Backend tests
echo "Running backend tests..."
cd backend
dotnet test
cd ..

# Frontend tests
echo "Running frontend tests..."
cd apps/flutter_app
flutter test
cd ../..

echo "Pre-commit checks complete."
