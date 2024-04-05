#!/usr/bin/env bash
# Requires the xmlstarlet and moreutils packages.
set -euxo pipefail

sed -i '\%//[[:space:]]*Runtime Version:%d' ChatThree/Resources/Language.Designer.cs

for f in ChatThree/Resources/Language.resx ChatThree/Resources/Language.*.resx; do
    sed -i 's/ xml:space="preserve"//g' "$f"
    xmlstarlet fo -e utf-8 -s 4 "$f" | sponge "$f"
done
