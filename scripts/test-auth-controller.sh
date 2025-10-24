#!/bin/bash
# Test script for AuthController
# Run AuthController tests with detailed output

echo "Running AuthController Tests..."
echo "================================"
echo ""

dotnet test --filter "FullyQualifiedName~AuthControllerTests" --logger "console;verbosity=detailed"

echo ""
echo "Test run complete!"
