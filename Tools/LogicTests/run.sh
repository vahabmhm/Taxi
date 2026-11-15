#!/bin/bash
# Compile check + logic tests without the Unity Editor.
# Needs mono (mcs, mono). UnityStub.cs is a small fake of the Unity API ( only for compiling and testing logic;
# PlayerPrefs is in memory and JsonUtility uses XmlSerializer ). It does NOT replace testing in Unity.
# Usage: Tools/LogicTests/run.sh   ( from the project root )
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${TMPDIR:-/tmp}/ecc-logic-tests"
mkdir -p "$OUT"
cd "$ROOT/Tools/LogicTests"
mcs -target:library -nowarn:0169,0649,0414,0067,0108,0114 -r:System.Runtime.Serialization.dll -out:"$OUT/UnityEngine.dll" UnityStub*.cs
# Adivery and AdsManagerNew need the real AndroidJava* API and the Tapsell DLL; their logic ( AdMediator ) is tested
find "$ROOT/Assets" -name "*.cs" -not -path "*/Editor/*" -not -path "*/Adivery/*" -not -name "AdsManagerNew.cs" -not -path "*/ExternalDependencyManager/*" -not -path "*/PlayServicesResolver/*" > "$OUT/files.txt"
for def in NONE UNITY_ADS UNITY_ANDROID; do
  echo "== compile with define $def"
  mcs -target:library -langversion:7 -define:$def -nowarn:0169,0649,0414,0067,0618,0219 -r:"$OUT/UnityEngine.dll" -out:"$OUT/Game.dll" @"$OUT/files.txt"
done
mcs -langversion:7 -nowarn:0219,0169,0649,0414,0067,0618 -r:"$OUT/UnityEngine.dll" -r:System.Xml.dll -r:System.Runtime.Serialization.dll -out:"$OUT/T.exe" @"$OUT/files.txt" T*.cs
cd "$OUT" && mono T.exe
