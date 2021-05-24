# Hugin



## How to execution

### Quick Start

```
git clone https://github.com/kjmtks/Hugin.git
cd Hugin
make local-up
# Then, open http://localhost:8080
```

### for Visual Studio

```
git clone https://github.com/kjmtks/Hugin.git
cd Hugin/Hugin
npm install
cd ..
# Then, open Hugin.sln and run with docker-compose profile
```

### for Production

```
git clone https://github.com/kjmtks/Hugin.git
cd Hugin
make pfx KEY="path to your key file" CER="path to your cert. file"
vim docker-compose.production.override.yml
make production-up
```
