# カスタム VM イメージの構築

VM を作成する際にアプリケーションのデプロイメントをスクリプトで含めることができますが、新しい VM を作成するたびにデプロイメント時間が増加するというデメリットがあります。代わりに、アプリケーションが既にデプロイされた VM から自分の VM イメージを作成することで、新しい VM を作成するたびにアプリケーションがすぐに利用可能になります。

このラボでは、VM を作成し、アプリケーションをデプロイした後、その VM からイメージを作成し、他の VM を作成するために使用します。

## 参照

- [Linux VM からのイメージ作成](https://docs.microsoft.com/ja-jp/azure/virtual-machines/linux/imaging)
- [Windows VM からのイメージ作成](https://docs.microsoft.com/ja-jp/azure/virtual-machines/windows/prepare-for-upload-vhd-image)
- [VM イメージ ビルダーの使用](https://docs.microsoft.com/ja-jp/azure/virtual-machines/image-builder-overview?tabs=azure-powershell)
- [`az image` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/image?view=azure-cli-latest)

## ベース VM の作成

イメージを作成するには、まずアプリケーションがデプロイされた VM を準備する必要があります。

IIS Web サーバーで実行される簡単な Windows アプリケーションをデプロイします。

📋 `labs-vm-image` というリソース グループを作成し、Windows Server 2022 Datacenter VM を作成します。

<details>
  <summary>方法がわからない場合</summary>

指定した場所で RG を作成します：



```
az group create -n labs-vm-image --tags courselabs=azure --location southeastasia
```


Windows の SKU を見つけます：



```
az vm image list-skus -l westus -p MicrosoftWindowsServer -f WindowsServer -o table
```


次に、選択したサイズ、場所、SKU で VM を作成します：



```
az vm create -l southeastasia -g labs-vm-image -n app01-base --image MicrosoftWindowsServer:WindowsServer:2022-datacenter-core-g2:latest --size Standard_D2s_v5 --admin-username labs --public-ip-address-dns-name <your-unique-dns-name> --admin-password <your-strong-password>
```


</details><br/>

リモート デスクトップ クライアントを使用して VM に接続し、アプリケーションの依存関係とアプリケーション自体をインストールします。

Windows Server Core を使用した場合はターミナル セッションに、完全な Windows Server を使用した場合は GUI になります。

PowerShell を使用して IIS Web サーバーをインストールします：



```
Install-WindowsFeature Web-Server,NET-Framework-45-ASPNET,Web-Asp-Net45
```


デフォルトの Web ページを削除し、アプリケーション Web ページをダウンロードします：



```
rm -fo C:\inetpub\wwwroot\iisstart.htm

curl -o C:/inetpub/wwwroot/default.aspx https://raw.githubusercontent.com/azureauthority/azure/main/labs/vm-image/app/default.aspx
```


アプリケーションをローカルでテストします。VM 名が含まれたシンプルな HTML が表示されるはずです：



```
curl.exe localhost
```


## イメージングのための VM 準備

このデモ アプリケーションのためにはこれで十分ですが、ベース VM を構築する際には、アプリケーションが必要とする設定をすべて行うことができます。

次に、Windows VM では [Sysprep ツール](https://learn.microsoft.com/ja-jp/windows-hardware/manufacture/desktop/sysprep--generalize--a-windows-installation?view=windows-11) を実行して、マシン固有の詳細を削除して一般化する必要があります。

コマンドを開始します：



```
C:\windows\system32\sysprep\sysprep.exe
```


以下を選択します：

- _Enter System Out-of-Box Experience (OOBE)_
- _Generalize_ にチェック
- _Shutdown_ を選択：

![Sysprep 画面で選択されたオプション](/img/sysprep.png)

> OK を押すと VM が一般化されてシャットダウンされます。リモート デスクトップ接続が失われます。

次に、イメージを作成するために VM を準備する必要があります。

📋 `az vm` コマンドを使用して、マシンをデアロケートし一般化します。

<details>
  <summary>方法がわからない場合</summary>

マシンをデアロケートすることで、Azure から見てシャットダウンされた状態になります：



```
az vm deallocate -g labs-vm-image -n app01-base
```


Sysprep で既に一般化した VM を Azure で一般化としてマークします：



```
az vm generalize -g labs-vm-image -n app01-base
```


</details><br/>

VM の詳細を表示して、イメージに使用する準備が整ったことを確認します：



```
az vm show --show-details -g labs-vm-image -n app01-base
```


> 電源状態が _VM deallocated_ で、パブリック IP がありません。

これで VM は準備完了です。

## VM からイメージを作成

イメージの作成は簡単です。作成したいイメージの名前と使用する VM を指定します。



```
# ヘルプ テキストを確認：
az image create --help

# ベース イメージが第 2 世代 (gen2) SKU である場合は、それも設定する必要があります：

az image create -g labs-vm-image -n app01-image --source app01-base --hyper-v-generation V2
```


これには時間がかかりません。実質的にイメージは OS ディスクへの参照にすぎません。イメージが準備できていることを確認します：



```
az image list -o table
```


> 完了したら、ポータルで VM イメージを確認します。VM の作成や新しいイメージの複製などのオプションがあります。

イメージは異なるライフサイクルを持つため、通常は別のリソース グループに保持することが望ましいです。アプリケーション RG を削除した場合でも、それらを保持したいと思うでしょう。

📋 `labs-vmss-win` という新しい RG を **ラボ RG と同じ場所に** 作成し、`az image` コマンドを使用してその RG にイメージをコピーします。

<details>
  <summary>方法がわからない場合</summary>

これは通常の RG です：


```
az group create -n labs-vmss-win --location southeastasia
```

The copy command takes source and target parameters:

```
az image copy --help

az image copy --source-type image --source-resource-group labs-vm-image --source-object-name app01-image  --target-location southeastasia --target-resource-group labs-vmss-win
```

</details><br/>

画像のコピーには時間がかかることがあります - AzureはOSディスクのスナップショットを取り、それを一時的なストレージアカウントにコピーします。最初はゆっくりと進みます - 1%... 2%... 3% - そして突然速度が上がります。

> この部分が完了するのを待つ必要はありません - 新しいターミナルウィンドウを開いて作業を続けてください。

> **ただし**、CLIから`KeyError: 'IMPORT_ENUM'`というエラーが出た場合、これは[既知の問題](https://github.com/Azure/azure-cli/issues/24263)です。ポータルに切り替えて、新しいRGにVMイメージを移動させてください。後でこのイメージを使用します。

通常の`vm create`コマンドを使用して、マーケットプレイスのURNの代わりにあなたのイメージ名を使用します。

これにより、ベースイメージから3つのVMが作成されます：



```
az vm create -g labs-vm-image -n app-n --image app01-image --size Standard_D2s_v5 --admin-username labs  --count 3 -l southeastasia --admin-password <strong-password>
```


VMの1つに公開IPアドレスを使用してアプリにアクセスしてみてください。

## Lab

NSGがトラフィックをブロックしているため、アプリにアクセスできません。ポート80を許可する新しいルールを追加し、各VMにアクセスできることを確認してください - 各々異なるVM名が表示されますが、同じページを見ることになります。これらは同じアプリケーションの三つのインスタンスです。単一のDNSアドレスを持ち、Azureがそれらの間でロードバランスをとるのが良いでしょう。ポータルで_Traffic Manager Profile_リソースを作成し、それを行うように設定してください。

> 詰まった場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## Cleanup

ラボのRGは削除してください **ただし、次のラボで使用するlabs-vmss-win RGにコピーしたイメージは削除しないでください**：



```
az group delete -y -n labs-vm-image
```
