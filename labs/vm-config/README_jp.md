# VM設定の自動化

新しく作成されたすべてのVMには、作成後に追加の設定が必要ですが、それを手動で続けるのは現実的ではありません。それは時間がかかり、エラーが発生しやすく、スケールしません。Azureでは、VMのデプロイメントの一部として、またはVMが作成された後にVMの設定を自動化するための複数のオプションを提供しています。

このラボでは、LinuxおよびWindows VMでデプロイメントスクリプトを実行するための簡単なオプションを使用します。

## 参考資料

- [Azure VM拡張機能](https://docs.microsoft.com/ja-jp/azure/virtual-machines/extensions/overview)
- [Azure VMアプリケーション](https://docs.microsoft.com/ja-jp/azure/virtual-machines/vm-applications-how-to?tabs=portal)
- [Azure VMのためのcloud-init](https://docs.microsoft.com/ja-jp/azure/virtual-machines/linux/cloud-init-deep-dive)
- [VM上でのコマンド実行](https://docs.microsoft.com/ja-jp/azure/virtual-machines/run-command-overview)

## ポータルでのVM設定を探索

ポータルを開き、新しい仮想マシンリソースを作成するために検索します。_高度_ タブに切り替えて、設定オプションを確認します。以下の3つの設定メカニズムがあります：

- **拡張機能**
- **アプリケーション**
- **cloud-initスクリプト**

これらはすべてVMを設定するためのメカニズムです。

## カスタムスクリプト拡張を使用したLinux

VM拡張機能はマシンが作成された後に追加されます。最も有用なのは、シェルスクリプトを実行できる[カスタムスクリプト拡張](https://docs.microsoft.com/ja-jp/azure/virtual-machines/extensions/custom-script-linux)です。

私たちは、WebサーバーにNginxをインストールするためにカスタムスクリプトを使用します。

リソースグループとVMを作成します：



```
az group create -n labs-vm-config --tags courselabs=azure -l southeastasia

az vm create -l southeastasia -g labs-vm-config -n web01 --image Ubuntu2204 --size <your-vm-size> --public-ip-address-dns-name <your-dns-name>
```


カスタムスクリプトはJSONで指定されます。ファイルURL、パスワードなどの機密設定を提供するための広範な[スキーマ](https://docs.microsoft.com/ja-jp/azure/virtual-machines/extensions/custom-script-linux#extension-schema)があります。

しかし、JSON文字列内のシェルコマンドで簡単に始めます：



```
{ "commandToExecute": "apt-get -y update && apt-get install -y nginx" }
```


📋 `vm extension` コマンドを使用して、JSON設定を使用してVMでカスタムスクリプトを実行します。

<details>
  <summary>
    わからない場合は？
  </summary>

ヘルプテキストを通じてナビゲートすると、`set` コマンドが拡張機能を適用することがわかります：



```
az vm extension --help

az vm extension set --help
```


すべての拡張機能に対して同じコマンドであるため、構文は少し複雑ですが、仕様は `settings` パラメーターに入ります。JSON文字列を変数に格納するのが最も簡単ですが、BashとPowerShellでは異なります：



```
# PowerShellでは：
$json='{ ""commandToExecute"": ""apt-get -y update && apt-get install -y nginx"" }'

# Bashでは：
json='{ "commandToExecute": "apt-get -y update && apt-get install -y nginx" }'
```


これで、シェルスクリプトを実行するためにカスタムスクリプト拡張機能を設定できます：



```
# 拡張機能を追加：
az vm extension set -g labs-vm-config --vm-name web01 --name customScript --publisher Microsoft.Azure.Extensions --settings "$json"
```


</details><br/>

CLIからの出力は拡張機能が追加されている間はあまり役に立ちませんが、ポータルでVMの_拡張機能 + アプリケーション_ ブレードで進捗を確認できます。

## Webサーバーをテスト

拡張機能が完了したら、VMにアクセスしてみますが、ポート80への受信トラフィックがブロックされているためアクセスできません。

NSGを使用してアクセスを管理します。CLIを使用してNG名を見つけ、ルールを表示します：



```
az network nsg list -g labs-vm-config -o table

az network nsg rule list -g labs-vm-config --include-default -o table --nsg-name <your-nsg-name>
```


> VMのデフォルトルールには、すべての受信トラフィックをブロックする _DenyAllInBound_ が含まれています。

NSGルールには優先順位番号があります。デフォルトの拒否ルールを上書きするために、より高い優先順位の別のルールを追加できます。

特定の公開アドレスからのアクセスを許可またはブロックするためにIP範囲を使用してルールを設定できます。Web VMにポート80での受信アクセスを許可する新しいルールを作成するには、以下を指定する必要があります：

- ソースアドレスプレフィックス、これは `Internet` のような特別な値で、公共インターネット上のすべてのアドレスを意味することができます
- 80ポートを使用するHTTPトラフィックのための目的地ポート

📋 Web VMにポート80での受信アクセスを許可する新しいNSGルールを作成します。

<details>
  <summary>わからない場合は？</summary>

`create` コマンドで新しいルールを追加します：



```
az network nsg rule create --help
```


ポート80でのアクセスを許可するために、デフォルトの拒否ルールよりも優先順位の高いルールを作成します：



```
az network nsg rule create -g labs-vm-config --nsg-name web01NSG -n http --priority 100 --source-address-prefixes Internet --destination-port-ranges 80 --access Allow
```

</details><br/>

</details><br/>

> ルールが追加されると、VMのDNS名にアクセスしてNginxのページを表示できます。

## Windows での VM 拡張機能

Windows VMも拡張機能をサポートしています。[Windows カスタム スクリプト 拡張機能](https://docs.microsoft.com/ja-jp/azure/virtual-machines/extensions/custom-script-windows)はLinuxバージョンと少し異なる設定ですが、アプローチは大まかに同じです。

より簡単な方法は `vm run-command` を使用することです。これはローカルのスクリプトファイルを読み込んでVM上で実行できます。[Powershell スクリプトファイル](/labs/vm-win/setup.ps1)を使用して、Windows VMに開発ツールをデプロイできます。

📋 新しい Windows 11 VM を作成し、カスタムスクリプト拡張機能を使用して開発ツールをデプロイしてください。

<details>
  <summary>わからない場合</summary>

まず、VMを作成します。アクセス可能なVMサイズ、最新のWindows 11イメージ、強力なパスワードを使用してください：


```
az vm create -l southeastasia -g labs-vm-config -n dev01 --image <windows-11-image> --size Standard_D4s_v5 --admin-username labs --public-ip-address-dns-name <your-unique-dns-name> --admin-password <your-strong-password>
```


VMが作成されたら、次のコマンドを実行します：



```
az vm run-command invoke  --command-id RunPowerShellScript -g labs-vm-config --name dev01 --scripts @labs/vm-win/setup.ps1
```


</details><br/>

> コマンドが完了すると、スクリプトからの出力が表示されます。あまり親切ではありませんが、`Chocolatey installed 3/3 packages`と表示されればセットアップが完了したことがわかります。

RDPクライアントを使用してVMに接続し、ツールがインストールされていることを確認できます。

## ラボ

`run-command`で完全なスクリプトを実行でき、スクリプトが不要なビルトインコマンドもいくつかあります。これはクイックデバッグに非常に便利です。

2つのVMを作成しましたが、ネットワークを介してそれらを接続するための何も指定していませんでした。同じリソースグループにあるので、どちらかといえば接続されていると思うかもしれません。LinuxとWindowsのVMでコマンドを実行して、IPアドレスを表示し、プライベートネットワークを介して互いに到達できるか確認してください。

> 困ったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

このコマンドで RG を削除し、すべてのリソースを削除します：



```
az group delete -y --no-wait -n labs-vm-config
```
