#!/bin/bash
set -e

# Change to the directory of the script
cd "$(dirname "$0")"

echo "----------------------------------------"
echo "🚀 Starting React Admin Page Pipeline"
echo "----------------------------------------"

# Ensure dependencies are installed
if [ ! -d "node_modules" ]; then
    echo "📦 Installing dependencies..."
    npm install
fi

echo "🔍 Running Linting..."
npm run lint

echo "🏗️ Building App..."
npm run build

echo "🧪 Running Tests with Coverage..."
npm run test:coverage

echo "✅ Pipeline completed successfully!"
