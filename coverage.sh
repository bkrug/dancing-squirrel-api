#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

rm -rf TestResults/coverage-report
dotnet-coverage collect --settings coverage.runsettings -f cobertura -o TestResults/coverage.cobertura.xml "dotnet test"
reportgenerator -reports:TestResults/coverage.cobertura.xml -targetdir:TestResults/coverage-report -reporttypes:Html
xdg-open TestResults/coverage-report/index.html
