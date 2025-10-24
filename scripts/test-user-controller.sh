#!/bin/bash
# Test script for UserController
# Run UserController tests with detailed output

echo "Running UserController Tests..."
echo "================================"
echo ""

dotnet test --filter "FullyQualifiedName~UserControllerTests" --logger "console;verbosity=detailed"

echo ""
echo "Test run complete!"
