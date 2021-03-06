#!/usr/bin/env bash

# Exit with error when any error occurs
set -e

function print_help {
    echo "This is a build script for FMark"
    echo ""
    echo "USAGE:"
    echo "  ./build.sh [OPTIONS]"
    echo ""
    echo "OPTIONS:"
    echo "  -b/--build    Build a specific project, can be set to"
    echo "                fsharp, js, all, testall"
    echo "  -h/--help     Print this help menu."
}

POSITIONAL=()
while [[ $# -gt 0 ]]
do
key="$1"

case $key in
    -b|--build)
    BUILD="$2"
    shift # past argument
    shift # past value
    ;;
    -h|--help)
    print_help
    exit 0
    ;;
    *)
    print_help
    exit 1
    ;;
esac
done

if [[ -z $BUILD ]]; then
    BUILD=all
fi

if [[ $BUILD != "fsharp" ]] && [[ $BUILD != "js" ]] && [[ $BUILD != "all" ]] && [[ $BUILD != "testall" ]]; then
    print_help
    exit 1
fi

echo "build set to: $BUILD"
echo ""

# get the current directory of the script, which is at the base of the project
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [[ -z $TRAVIS_BUILD_DIR ]]; then
    BASE_DIR=$DIR
else
    echo "Running on travis-ci"
    BASE_DIR=$TRAVIS_BUILD_DIR
fi

function cd_run_module() {
    echo "########## Running $1 module tests ###########"
    cd $BASE_DIR/FMark/src/Common/$1
    dotnet build
    dotnet run --no-build -- --sequenced
    if [[ "$?" != "0" ]]; then
        exit 1
    fi
}

if [[ $BUILD = "testall" ]]; then
    modules=("Lexer" "TOCite" "Markalc" "Parser" "HTMLGen" "MarkdownGen" "Logger")
    for i in "${modules[@]}"; do
        cd_run_module $i
    done
fi

if [[ $BUILD = "testall" ]] || [[ $BUILD = "all" ]] || [[ $BUILD = "fsharp" ]]; then
    echo "Running F# tests"
    cd $BASE_DIR/FMark/src/FMarkCLI
    dotnet build
    dotnet run --no-build -- --test true -l info
    if [[ "$?" != "0" ]]; then
        exit 1
    fi
fi

if [[ $BUILD = "all" ]] || [[ $BUILD = "js" ]]; then
    echo "Running javascript build"
    cd $BASE_DIR/FMark
    yarn install
    cd $BASE_DIR/FMark/src/FMarkFable
    dotnet restore
    dotnet fable yarn-dev
    if [[ "$?" != "0" ]]; then
        exit 1
    fi
fi
