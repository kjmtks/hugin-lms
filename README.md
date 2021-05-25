# Hugin

任意の言語のプログラミング演習をウェブブラウザで実施できる LMS です．
プログラミング演習のほか，自由記述形，ファイル提出，フォーム入力の課題を課すこともできます．

コンテンツはテキストで記述します，このシステム自体がGitのリモートリポジトリとしても振る舞い，
GitHubのようにGitのコマンドでコンテンツの入手・更新ができます．
また，ウェブからもコンテンツの編集が可能です．

そのほか，以下の機能を有しています:

・単体テストによる課題の自動チェック
・学生の学習行動の可視化・全行動の記録

## 実行方法

### Quick Start

試用する場合はこの方法がお勧めです．

Docker, docker-compose が利用可能であれば，macOS, Windows, Linux のいずれのOSでも動作します．
Windows の場合は WSL2 + Docker Dektop での動作を確認しています．

ソースコード入手と実行:
```
git clone https://github.com/kjmtks/hugin-lms.git
cd Hugin
make local-up
# Then, open http://localhost:8080
```

終了手順:
```
make local-down
```

### for Visual Studio (Windows)

開発者向けです．

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

HTTPS での本番環境で動作させる場合です．
サーバー証明書とその鍵が必要です．

Requirements:
* Docker, docker-compose
* Server certification and its key

ソースコード入手と準備:
```
git clone https://github.com/kjmtks/hugin-lms.git
cd Hugin
make pfx KEY="path to your key file" CER="path to your cert. file"
vim docker-compose.production.override.yml
```

起動手順:
```
make production-up
```

終了手順:
```
make production-down
```
