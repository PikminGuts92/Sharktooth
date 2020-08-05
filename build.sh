BUILD_MODE="Release"
BUILD_VERSION=${1:-"0.0.0.0"}
OUTPUT_PATH="./BUILD"
ZIP_PATH="$OUTPUT_PATH/Sharktooth.zip"

# Clear previous build
rm $OUTPUT_PATH -rf

# Build + publish projects
dotnet publish -c $BUILD_MODE -o $OUTPUT_PATH -p:Version=$BUILD_VERSION

# Delete debug + config files
rm ./$OUTPUT_PATH/*.config -f
rm ./$OUTPUT_PATH/*.pdb -f

# Copy licences + README
cp ./LICENSE ./$OUTPUT_PATH/LICENSE -f
cp ./THIRDPARTY ./$OUTPUT_PATH/THIRDPARTY -f
cp ./README.md ./$OUTPUT_PATH/README.md -f

# Zip everything up
zip $ZIP_PATH $OUTPUT_PATH -jr