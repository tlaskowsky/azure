# 仮想マシン - Windows

VM はサーバーだけでなく、ワークステーションマシンとしても役立ちます。どこからでもアクセスできる開発マシンを持つことは便利です - 必要な開発ツールをすべて備えた強力な VM を設定し、実際に使用しているときのみ支払うことができます。

このラボでは、Windows VM を作成し、VM に接続してセットアップスクリプトを実行することで、標準的な開発ツールを手動で構成する方法を見ていきます。

## 参考文献

- [Azure Virtual Machine ドキュメント](https://docs.microsoft.com/ja-jp/azure/virtual-machines/)

- [`az vm` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/vm?view=azure-cli-latest)

- [`az vm image` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/vm/image?view=azure-cli-latest)


## ポータルで Windows VM を探索する

ポータルを開いて、新しい Virtual Machine リソースを作成するために検索します。イメージを Windows OS に変更します。何が変わりますか？

- 認証モデルはユーザー名 + パスワードに切り替わります
- デフォルトの着信ポートは 3389 に設定され、リモートデスクトップアクセス用になります

_ディスク_ セクションに進んで、VM にアタッチする新しい仮想ディスクを作成できます。

## CLI で Windows VM を作成する

まず、新しい VM リソースが属するリソース グループを作成する必要があります。

_グループを作成します - 好きな場所を使用してください：_



```
az group create -n labs-vm-win --tags courselabs=azure -l southeastasia
```


Windows は Linux よりも要求が厳しい OS です...

_使用できる大きな VM サイズを探します：_



```
# PowerShell で：
az vm list-sizes -o table --query "[?numberOfCores==``4`` && memoryInMb==``16384``]" --location "southeastasia"

# Bash で：
az vm list-sizes -o table --query "[?numberOfCores==\`4\` && memoryInMb==\`16384\`]" --location "southeastasia"
```


> D シリーズは汎用マシンであり、`Standard_D4s_v5` のようなオプションがあります

OS イメージには _URN_ と呼ばれる完全な名前があり、以下で構成されています：

- _パブリッシャー_（例：Microsoft や Canonical）
- _オファー_（例：Ubuntu Server や Windows 11）
- _SKU_（例：Windows 11 Pro や Ubuntu Server LTS）
- _バージョン_（OS リリースのバージョン番号）

使用する OS イメージを見つけるには、`vm image list` コマンドがあります。`offer` オプションでフィルタリングできます：



```
# Windows Desktop のすべてのオファーを表示：
az vm image list-offers --publisher MicrosoftWindowsDesktop --location southeastasia -o table

# Windows 11 のすべての SKU を表示：
az vm image list-skus -l westus -f windows-11 -p MicrosoftWindowsDesktop -o table

# Windows 11 Pro のすべてのイメージを表示：
az vm image list --sku win11-22h2-pro  -f windows-11 -p MicrosoftWindowsDesktop --location southeastasia -o table --all
```

📋 `vm create` コマンドを使用して Windows 11 VM を作成します。IP アドレスを使用せずにマシンにアクセスできるように、DNS 名を含めてください。

<details>
  <summary>わからない場合</summary>

ヘルプテキストで DNS 名パラメーターまでたどります：



```
az vm create --help
```


Windows VM にはもう少し情報が必要です - 指定する必要があります：

- 管理者ユーザー名
- 管理者パスワード

これで始められます - Windows 11 イメージの正確なバージョンを使用できます。URN は次のようになります：_MicrosoftWindowsDesktop:windows-11:win11-22h2-pro:22621.674.221008_

または、最新バージョンだけを使用したい場合は、バージョン番号を _latest_ に置き換えます。



```
# パスワードは強力である必要があります：
az vm create -l southeastasia -g labs-vm-win -n dev01 --image <image-urn> --size Standard_D4s_v5 --admin-username labs --public-ip-address-dns-name <your-unique-dns-name> --admin-password <your-strong-password>
```

<
</details><br/>

> Windows Desktop VM の作成には、Linux Server VM よりも時間がかかることがあります。

実行中にポータルを開き、VM と一緒に作成されたリソースを確認します：

- Linux VM と同じですか？
- リモートデスクトップポート（3389）はどこで VM へのアクセスを許可するように設定されていますか？

VM のサポートリソースには、Windows の C: ドライブになる OS ディスクが含まれています。

## VM にデータディスクを追加する

VM を削除するときに通常は OS ディスクも削除しますが、データを保持したい場合は、別のディスクを作成して VM にアタッチできます。

VM のディスクは `vm disk` コマンドを使用して管理します：



```
az vm disk attach --help
```

📋 Windows VM に新しい 2TB のプレミアムディスクを追加します。

<details>
  <summary>わからない場合</summary>

`sku` パラメーターはディスクのパフォーマンスを指定し、サイズは GB で設定する必要があり、`new` フラグでディスクを作成します：



```
az vm disk attach -g labs-vm-win --vm-name dev01 --name dev01data --new --sku Premium_LRS --size-gb 2048
```

</details><br/>

> プレミアム ストレージは、データセンターの高速なソリッドステートディスクを使用しているため、標準ディスクよりもパフォーマンスがはるかに良いです。

ディスクは VM とは別に課金され、大容量のプレミアム ストレージ ディスクは高価になることがあります。プレミアムディスクが付いた割り当て解除された VM は、VM のコンピューティングコストは発生しませんが、ディスクのストレージコストは依然として発生します。

## 開発ツールを接続してインストール

リモート デスクトップ クライアントを使用して VM に接続できます：

- Windows の場合 - 組み込みのリモート デスクトップ接続アプリを使用
- Mac の場合 - App Store から Microsoft Remote Desktop をインストール
- Linux の場合 - [Remmina](https://remmina.org) が良い選択肢です

DNS 名と管理者資格情報を使用して VM に接続します。最終的なインストール手順を含む Windows セッションが開始されます。

[setup.ps1](setup.ps1) PowerShell スクリプトを VM にコピーします（自分のマシンからコピーして貼り付けるか、VM で [GitHub](https://raw.githubusercontent.com/azureauthority/azure/main/labs/vm-win/setup.ps1) からダウンロードします）。次に、_管理者_ PowerShell セッションでスクリプトを実行します。これにより Git と VS Code がインストールされ、作業の準備が整います。

## ラボ

VM 上の Windows エクスプローラーを開き、マシンの設定を確認します。1つのディスクしかないことがわかります。しかし、Azure ポータルで確認すると、VM に 2番目のデータディスクが接続されていることがわかります。それはそこにありますが、OS を初期化するために設定する必要があります。

> 困ったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

このコマンドで RG を削除し、すべてのリソースを削除します：



```
az group delete -y -n labs-vm-win
```
