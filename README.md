# Hugin

任意の言語のプログラミング演習をウェブブラウザで実施できる LMS です．
プログラミング演習のほか，自由記述形，ファイル提出，フォーム入力の課題を課すこともできます．

コンテンツはテキストで記述します，このシステム自体がGitのリモートリポジトリとしても振る舞い，
GitHubのようにGitのコマンドでコンテンツの入手・更新ができます．
また，ウェブからもコンテンツの編集が可能です．

そのほか，以下の機能を有しています:
・単体テストによる課題の自動チェック
・学生の学習行動の可視化・全行動の記録

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
