#!/usr/bin/env bash
# Verify every <code source="..."/> reference in Hiero XML doc comments resolves
# to a non-empty snippet. DocFX silently renders empty code blocks when a region
# name is wrong, so this script catches that class of bug before publish.
#
# Prereqs:
#   - dotnet tool restore  (installs docfx from dotnet-tools.json)
#   - Run from repo root.

set -euo pipefail

cd "$(dirname "$0")/.."

echo "==> Building Hiero (produces Hiero.xml with doc comments)"
dotnet build src/Hiero/Hiero.csproj -p:GenerateDocumentationFile=true \
  -v:q -nologo > /dev/null

echo "==> Regenerating DocFX API metadata (resolves <code source/region>)"
rm -f docfx/api/Hiero.*.yml
dotnet docfx metadata docfx/docfx.json > /dev/null

fail=0

echo "==> Checking every YAML for empty <pre><code> blocks"
# An empty code block after resolution means: region name was wrong, or the
# source file no longer contains the named region.
while IFS= read -r yml; do
  if grep -q '<pre><code class="lang-csharp"></code></pre>' "$yml"; then
    echo "  [FAIL] $yml contains an empty code block — check <code source/region> on the referenced type"
    fail=1
  fi
done < <(find docfx/api -name 'Hiero.*.yml' -type f)

echo "==> Counting <example> tags in generated XML"
example_count=$(grep -c '<example' src/Hiero/bin/Debug/net10.0/Hiero.xml || true)
echo "    <example> tags: $example_count"

echo "==> Counting <code source=...> references in source"
src_count=$(grep -rE 'code source=' src/Hiero --include='*.cs' | wc -l)
echo "    <code source=\"...\"/> references: $src_count"

if [[ $fail -eq 0 ]]; then
  echo "OK — all snippet references resolve to non-empty code blocks."
else
  echo "FAIL — one or more snippet references resolved to empty code blocks."
  exit 1
fi
