#!/bin/bash
set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Valora Quick Setup ===${NC}\n"

# 1. Check Prerequisites
echo -e "${YELLOW}[1/4] Checking prerequisites...${NC}"
if ! command -v docker &> /dev/null; then
    echo "Error: Docker is not installed or not in PATH."
    exit 1
fi
echo -e "${GREEN}✓ Docker found${NC}"

if ! command -v dotnet &> /dev/null; then
    echo "Warning: .NET SDK not found. You will need it to run the backend."
fi

if ! command -v flutter &> /dev/null; then
    echo "Warning: Flutter SDK not found. You will need it to run the app."
fi

# 2. Setup Backend Environment
echo -e "\n${YELLOW}[2/4] Configuring Backend...${NC}"
if [ ! -f backend/.env ]; then
    echo "Copying .env.example to backend/.env..."
    cp backend/.env.example backend/.env
else
    echo "backend/.env already exists. Skipping copy."
fi

# Generate JWT Secret if needed
current_secret=$(grep "JWT_SECRET=" backend/.env | cut -d '=' -f2)
if [[ -z "$current_secret" || "$current_secret" == "Your_Strong_Production_Secret_Here_At_Least_32_Chars" || "$current_secret" == "change_me_to_a_long_random_string" ]]; then
    echo "Generating secure JWT_SECRET..."
    # Generate a random 32-byte hex string (64 chars)
    new_secret=$(openssl rand -hex 32)

    # Use perl for in-place editing to avoid BSD/GNU sed differences
    # Escape special characters if any (hex is safe)
    perl -pi -e "s/JWT_SECRET=.*/JWT_SECRET=$new_secret/" backend/.env
    echo -e "${GREEN}✓ Updated JWT_SECRET in backend/.env${NC}"
else
    echo -e "${GREEN}✓ JWT_SECRET is already configured${NC}"
fi

# 3. Setup Frontend Environment
echo -e "\n${YELLOW}[3/4] Configuring Frontend...${NC}"
if [ ! -f apps/flutter_app/.env ]; then
    echo "Copying .env.example to apps/flutter_app/.env..."
    cp apps/flutter_app/.env.example apps/flutter_app/.env
    echo -e "${GREEN}✓ Created apps/flutter_app/.env${NC}"
else
    echo "apps/flutter_app/.env already exists. Skipping copy."
fi

# 4. Instructions
echo -e "\n${BLUE}=== Setup Complete ===${NC}"
echo -e "You are ready to run Valora! Follow these steps in separate terminals:\n"

echo -e "${YELLOW}1. Start Infrastructure (Database):${NC}"
echo -e "   docker-compose -f docker/docker-compose.yml up -d\n"

echo -e "${YELLOW}2. Start Backend:${NC}"
echo -e "   cd backend"
echo -e "   dotnet run --project Valora.Api\n"

echo -e "${YELLOW}3. Start Frontend:${NC}"
echo -e "   cd apps/flutter_app"
echo -e "   flutter pub get"
echo -e "   flutter run\n"

echo -e "${GREEN}Happy Coding!${NC}"
