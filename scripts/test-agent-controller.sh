#!/bin/bash
# Test script for AgentController
# Run AgentController tests with detailed output

echo "Running AgentController Tests..."
echo "================================="
echo ""

dotnet test --filter "FullyQualifiedName~AgentControllerTests" --logger "console;verbosity=detailed"

echo ""
echo "Test run complete!"
