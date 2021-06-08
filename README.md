# Hugin

任意の言語のプログラミング演習をウェブブラウザで実施できる LMS です．
プログラミング演習のほか，自由記述形，ファイル提出，フォーム入力の課題を課すこともできます．

コンテンツはテキストで記述します，このシステム自体がGitのリモートリポジトリとしても振る舞い，
GitHubのようにGitのコマンドでコンテンツの入手・更新ができます．
また，ウェブからもコンテンツの編集が可能です．

そのほか，以下の機能を有しています:

* 単体テストによる課題の自動チェック
* 提出課題への手動フィードバック，最終成績のCSV出力
* 任意のプログラム実行環境の構築
* 学生の学習行動の可視化，オンライン状態の監視，全行動の記録，
* LDAP認証の利用（LDAPを使用しない認証との併用も可）
* スタイラスペンによるページへの書き込み
* 講義コンテンツ，プログラム実行環境(サンドボックス)，課題(アクティビティ)のひな形を[ウェブ](https://github.com/kjmtks/hugin-hub/blob/main/hub.yaml)からインポート

また，以下を準備しています:

* 独自のクエリ式を用いた検索
* 行動の可視化の強化
* 単体テスト機能の強化


## 実行方法

以下の3種類の実行方法を用意しています．
いずれも Docker と docker-compose が必要です．
Windows の場合は　[WSL2](https://docs.microsoft.com/ja-jp/windows/wsl/install-win10) と [Docker Desktop](https://docs.microsoft.com/ja-jp/windows/wsl/tutorials/wsl-containers#install-docker-desktop) を準備しておいてください．


初期ユーザーのアカウントとパスワード:

アカウント | パスワード
----------|-----------
admin     | password
test001   | password
test002   | password
test003   | password


### Quick Start

[![](https://img.youtube.com/vi/Yvm4sSdc58M/0.jpg)](https://www.youtube.com/watch?v=Yvm4sSdc58M)

試用する場合，またはローカル環境で動作させる場合はこの方法がお勧めです．
`make` コマンドでシステムの起動と終了ができるようにしています．

Docker, docker-compose が利用可能であれば，macOS, Windows, Linux のいずれのOSでも動作します．
Windows の場合は WSL2 + Docker Dektop での動作を確認しています．

#### ソースコード入手と実行

```
git clone https://github.com/kjmtks/hugin-lms.git
cd Hugin
make local-up
```

`make` コマンド実行後，しばらくしてから `http://localhost:8080` にブラウザでアクセスしてください．
ただし，Internet Explorerは対応していません．

#### 起動後~デモを動かすまで

ログインが必要です．
まずは `admin` でログインをしてください．
パスワードは `password` です．

デモ用の講義「Demo Lecture」が用意されていますが，
最初に，この講義に必要なプログラム実行環境（サンドボックス）をインストールする必要があります．
「担当」→「講義」→「Demo Lecture」のリンクから，Demo Lecture の管理画面に移動してください．
インストールが必要なサンドボックス一覧が表示されていますので，それぞれ「インストール」ボタンをクリックしてインストールを開始してください．

「講義ページへ」ボタンをクリックすると学生に提示する講義のページへ移動することができます(各ページの初回アクセス時にページ生成のための時間がかかりますが，キャッシュをとっているため，以降はあまり時間はかかりません)．
サンドボックスのインストールが完了した後に，プログラム演習を行うことができます．


#### 終了手順

```
make local-down
```

### Azure上での試用

Ubuntu のインスタンスを作成します．
22(ssh) と 8080 のポートを解放しておいてください．
ssh でインスタンスにログイン後，以下を実行します:

```
sudo apt update && sudo apt install -y docker.io docker-compose make
git clone https://github.com/kjmtks/hugin-lms.git
cd hugin-lms
sudo systemctl status docker
sudo make local-up
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
`make` コマンドでシステムの起動と終了ができるようにしています．

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
