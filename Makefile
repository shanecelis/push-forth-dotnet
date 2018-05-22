INSTALL_LOCATION = /Users/shane/unity/Eye\ Shader/Assets/
INSTALL_LOCATION2 = /Users/shane/unity/One\ Jar/Assets/
LIBRARIES = $(wildcard src/bin/Debug/netstandard2.0/*.dll)
.PHONY: all build clean test run

all: build

build:
	dotnet build

test:
	cd tests && dotnet test

clean:
	dotnet clean

sample-run: all
	$(RUN_EXE) ./$(EXECUTABLE) gpsamples/intreg1.pushgp

install: build
	cp $(LIBRARIES) $(INSTALL_LOCATION)
	cp $(LIBRARIES) $(INSTALL_LOCATION2)

# doc:
# 	doxygen Doxyfile.txt
