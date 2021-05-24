# Hugin



## How to execution

### Quick Start

Requirements:
* Docker, docker-compose

```
git clone https://github.com/kjmtks/hugin-lms.git
cd Hugin
make local-up
# Then, open http://localhost:8080
```

### for Visual Studio (Windows)

Requirements:
* Visual Studio 2019
* Docker Desktop

```
git clone https://github.com/kjmtks/hugin-lms.git
cd Hugin/Hugin
npm install
cd ..
# Then, open Hugin.sln and run with docker-compose profile
```

### for Production

Requirements:
* Docker, docker-compose
* Server certification and its key

```
git clone https://github.com/kjmtks/hugin-lms.git
cd Hugin
make pfx KEY="path to your key file" CER="path to your cert. file"
vim docker-compose.production.override.yml
make production-up
```
