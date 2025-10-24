#!/bin/bash
# Test script for PluginConfigController
# Run PluginConfigController tests with detailed output

echo "Running PluginConfigController Tests..."
echo "========================================"
echo ""

dotnet test --filter "FullyQualifiedName~PluginConfigControllerTests" --logger "console;verbosity=detailed"

echo ""
echo "Test run complete!"
