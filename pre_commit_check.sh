#!/bin/bash
# Pre-commit checklist
echo "Running pre-commit checks..."

# Backend
echo "Running Backend Tests..."
dotnet test backend/Valora.UnitTests

# Frontend
echo "Running Frontend Analysis..."
cd apps/flutter_app
flutter analyze
echo "Running Frontend Tests..."
flutter test
cd ../..

echo "Pre-commit checks complete."
