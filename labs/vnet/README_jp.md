# 仮想ネットワーク

仮想ネットワークはAzureにプライベートです - サービスは公開インターネット上でアクセス可能でなくても仮想ネットワーク（vnet）内で互いに通信できます。

VnetはAzureで安全なソリューションを展開するための核心コンポーネントであり、使用しているサービスがそれをサポートしていれば、すべてのアプリケーションでの使用を目指すべきです。まずvnetを作成し、その中に他のサービスをデプロイします。通常、リソースをvnet間で移動することはできませんので、ネットワーキングを事前に計画する必要があります。

## 参照

- [仮想ネットワーク概要](https://docs.microsoft.com/ja-jp/azure/virtual-network/)
- [`az network` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/network?view=azure-cli-latest)
- [`az network vnet` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/network/vnet?view=azure-cli-latest)
- [`az network vnet subnet` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/network/vnet/subnet?view=azure-cli-latest)

## ポータルで仮想ネットワークを探索

ポータルを開いて、新しい仮想ネットワークリソースを作成するための検索を行います。他のサービスほど多くのオプションはありません：

- 名前はグローバルにユニークである必要はなく、RG内でユニークであればよい
- IPアドレス - vnet全体のアドレス範囲を選択する必要があります（[プライベートCIDR範囲](https://ja.wikipedia.org/wiki/プライベートネットワーク#プライベートIPv4アドレス)から）
- すべてのvnetには少なくとも1つのサブネットが必要であり、サブネットはvnet範囲内に自身のIP範囲を持ちます
- 単一のvnet内でワークロードを分離するために複数のサブネットを作成できます

CLIに戻って、その中にvnetといくつかのサービスを作成します。

## CLIで仮想ネットワークを作成

ラボ用の新しいリソースグループを好みのリージョンで開始します：


```
az group create -n labs-vnet --tags courselabs=azure -l eastus
```


📋 `network vnet create` コマンドを使用して新しいvnetを作成します。名前を `vnet1` とし、アドレス空間は `10.10.0.0/16` を使用します。

<details>
  <summary>わからない場合は？</summary>

ヘルプを始めてください：



```
az network vnet create --help
```


RG、vnet名、およびアドレスプレフィックスを指定する必要があります：



```
az network vnet create -g labs-vnet -n vnet1 --address-prefix "10.10.0.0/16"
```


</details><br/>

ポータルでvnetを作成するとき、デフォルトでサブネットが作成されます。基本的な `vnet create` コマンドではサブネットを提供しません：



```
az network vnet show -g labs-vnet -n vnet1
```


実際にサービスをデプロイするのはサブネットなので、vnet内に少なくとも1つは必要です。

📋 `vnet subnet create` でvnet内に2つのサブネットを作成します。名前は `frontend` と `backend` とし、アドレス範囲は `10.10.1.0/24` と `10.10.2.0/24` を使用します。

<details>
  <summary>わからない場合は？</summary>

サブネットには独自のヘルプテキストがあります：



```
az network vnet subnet create --help
```


RG、vnet、サブネット名、およびアドレス範囲を指定する必要があります：


```
az network vnet subnet create -g labs-vnet --vnet-name vnet1 -n frontend --address-prefix "10.10.1.0/24"

az network vnet subnet create -g labs-vnet --vnet-name vnet1 -n backend --address-prefix "10.10.2.0/24"
```


</details><br/>

> サブネットでは親vnetにない範囲のIPアドレス範囲や重複するIPアドレス範囲を使用することはできません。CLIはそれを強制しますか？

## VNet内に仮想マシンを作成

[VMラボ](/labs/vm/README.md)で仮想マシンについて説明しました - それらはIaaSアプローチであり、Azureでアプリを実行するためのより良い方法が通常あります。しかし、それらは扱いやすく、vnet内のネットワーキングを確認するために使用できます。

Ubuntu Serverを実行するLinux VMを作成します：



```
az vm create -g labs-vnet -n vm01 --image UbuntuLTS --vnet-name vnet1 --subnet frontend --generate-ssh-keys
```


このコマンドは、リモートマシンにログインするために[SSH](https://ja.wikipedia.org/wiki/SSH)の設定を行います。出力では、接続に使用する公開IPアドレスが表示されます。

📋 Windows VMを作成したい場合、異なるイメージを使用する必要があります。`az` コマンドで Windows Server 2019 イメージ名を見つけることができますか？

<details>
  <summary>わからない場合は？</summary>

利用可能なVMイメージを操作するには `az vm image` コマンドを使用します：


```
az vm image list --help
```


すべてのイメージをリストするのには時間がかかるので、`offer` パラメータを使用してOS名でフィルタリングできます：



```
az vm image list --offer  Windows -o table
```


多くのイメージが長い名前で表示されますが、`vm create` コマンドではエイリアスを使用できます。Windows Server 2019イメージは `Win2019Datacenter` と呼ばれています。

</details><br/>

## VMに接続

`vm create` コマンドの完了にはそう時間はかかりません。完了すると、Linux VMは使用準備が整います。

📋 `az vm` コマンドを使用して `vm01` の公開IPアドレスを印刷します。

<details>
  <summary>わからない場合は？</summary>

`show` コマンドはリソースについての基本情報を印刷します：


```
az vm show -g labs-vnet -n vm01
```


多くのデータが表示されますが、公開IPアドレスは表示されません。`az vm show --help` を実行すると `--show-details` オプションがあることがわかります。公開IPアドレスのみを印刷するクエリと共にそれを使用できます：


```
az vm show -g labs-vnet -n vm01 --show-details --query publicIps -o tsv
```


</details><br/>

次に、`ssh` を使用してVMに接続します（macOS、Linux、Windows 10+にデフォルトでインストールされています）：



```
ssh <vm01-public-ip>

# 今VMセッションで：
ip address

# 自分のマシンに戻るには：
exit
```


VMはvnet上のローカルIPアドレスについてのみ知っています。公開IPアドレスはマシンの外側で管理されます；プライベートIPはvnetによって割り当てられ、`10.10.1.x` 範囲でそれを見るべきです。

## ポータルでネットワーキングを探索

`az` コマンドでいくつかのリソースを作成しました。ポータルでリソースグループを開くと、私たちが明示的に作成しなかったオブジェクトが表示されます：

- VMに接続された仮想ストレージユニットであるディスク
- VMをvnetに接続するNIC
- VMへのネットワークアクセスを制御するネットワークセキュリティグループ
- 公開IPアドレス

> _リソースビジュアライザー_ をクリックして、すべてのリソースがどのように関連しているかを見ます。

これらはすべてデフォルトの設定で作成されましたが、より多くの制御が必要な場合は `az` コマンドを使用して独立してそれらを作成および管理できます。

## ラボ

`az` コマンドは素晴らしいツールですが、1つ欠点があります：それは_命令型_アプローチです。リソースを作成するとき、Azureに何をするべきかを指示しますが、スクリプトを再実行する必要がある場合には、すでに存在するリソースに関するエラーを取り扱うのが難しくなります。

Azureは、最終結果がどのようであるべきかを記述する_宣言型_アプローチもサポートしています。これらは[Azureリソースマネージャー(ARM)テンプレート](https://docs.microsoft.com/ja-jp/azure/azure-resource-manager/templates/)です - これらを繰り返し実行しても常に同じ結果を得られます。

`labs-vnet`リソースグループのARMテンプレートをエクスポートしてみてください。`labs-vnet2`という新しいRGにリソースのコピーをデプロイするためにそれを使用できますか？

> 詰まったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

このラボのためのRGを削除すると、すべてのリソースが削除されます：


```
az group delete -y -n labs-vnet

az group delete -y -n labs-vnet2
```
