# Azureファイルストレージ

Azureファイルは、ファイルシステムにマウントできるストレージサービスです。標準的なSMBプロトコルを使用してコンポーネント間でファイルを共有する良い方法です。

このラボでは、ファイル共有を作成し、ローカルマシンとAzure VMにマウントする方法を見ていきます。

## 参照

- [Azureファイルの概要](https://docs.microsoft.com/ja-jp/azure/storage/files/storage-files-introduction)

- [`az storage share` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/storage/share?view=azure-cli-latest)

- [`az storage file` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/storage/file?view=azure-cli-latest)

- [Microsoft Learn: AzureファイルとAzureファイル同期の設定](https://docs.microsoft.com/ja-jp/learn/modules/configure-azure-files-file-sync/)

## ファイル共有の作成

Azureファイルはストレージアカウントの機能です。まずはRGとSAを作成しましょう：



```
az group create -n labs-storage-files --tags courselabs=azure -l southeastasia 

az storage account create -g labs-storage-files --sku Standard_LRS -n <sa-name>
```


> AzureファイルはBlobストレージと同じパフォーマンスと冗長性のオプションを持っています

📋 `storage share` コマンドを使って`labs`という新しいファイル共有を作成してください。

<details>
  <summary>わからない場合は？</summary>


```
az storage share create --help

az storage share create -n labs --account-name <sa-name>
```


</details><br/>

ポータルで共有を確認します。_階層_ と_クォータ_ はデフォルト値を使用しています。

共有を開くと、Blobと似た方法でファイルを操作できます：

- `uploads`という新しいディレクトリを作成する
- ラボフォルダの[document.txt](/labs/storage-files/document.txt)を`uploads`ディレクトリにアップロードする
- ファイルの詳細を開く

省略記号(`...`)をクリックすると、ポータルで直接ファイルの内容を表示・編集できます。URLがあるのが見えます。

エディタには_ダウンロード_リンクがあります。URLから直接ファイルをダウンロードしてみてください：



```
curl -o download.txt https://<sa-name>.file.core.windows.net/labs/uploads/document.txt

cat download.txt
```


> XMLエラー文字列が表示されます。ファイル共有はデフォルトで公開アクセスが許可されていません

ファイル共有にHTTPアクセスを与えることは可能ですが、アカウントレベルでSASトークンを生成する必要があります。通常は代わりに共有をローカルファイルシステムにマウントします。

## 共有のマウントとキーのローテーション

ポータルで共有に戻り、_接続_をクリックします。Windows、macOS、Linuxで共有をマウントするための指示が表示されます。

例えばMacの指示は次のようになります：



```
open smb://<sa-name>:<sa-key>@<sa-name>.file.core.windows.net/<share-name>
```


ローカルマシンで共有をマウントします。document.txtが見えること、内容を編集できることを確認してください。ポータルで再び開いて、変更が反映されていることを確認してください。ポータルで変更を加え、ローカル共有でそれが見えることを確認してください。

> ファイルシステムの機能について興味深いメッセージが表示されるかもしれません - SMBにはネイティブOSのファイルシステムのすべての機能はありません。

共有への認証はストレージアカウントキーを使用します - これはアカウントが作成されたときに自動生成されます。これにより、ストレージアカウント全体へのアクセスが可能になります。

ポータルでSAを確認し、_アクセスキー_を開きます。_key1_ と _key2_ があり、それぞれローテートするオプションがあります。キーを共有する場合は定期的にローテーションする必要があります - クライアントは新しいキーを知る必要があります。



```
az storage account keys list --account-name <sa-name>
```


キー1の値が共有をマウントするために使用したものであることがわかります。キーを更新し、アクセスキーが新しいものに置き換わります：



```
az storage account keys renew --key primary -g labs-storage-files -n <sa-name> 
```


ローカル共有で再度ファイルを開いてみてください。OSによってはエラーメッセージが表示されるかもしれませんが、失敗します。キーの更新は古いキーでの認証を無効にします。新しいキーで再接続する必要があります。

## VMでの共有のマウント

VMでの共有のマウントは同じプロセスです。新しいVMが作成されたときに共有にすぐにアクセスできるように、スクリプトでそれをキャプチャして実行することができます。

このスクリプトはLinux VM用です - あなたの詳細で更新する必要があります：

- [cloud-init/mount-share.sh](/labs/storage-files/cloud-init/mount-share.sh) - ファイルを開いて`<sa-name>`と`<sa-key>`をあなた自身のアカウント名とキーに置き換えてください

スクリプトを編集したら、cloud-initを使用してスクリプトを実行し、共有をマウントするVMを作成します：


```
az vm create -g labs-storage-files -n vm01 --image Ubuntu2204 --custom-data @labs/storage-files/cloud-init/mount-share.sh --generate-ssh-keys
```


VMに接続し、ファイルを読み書きできることを確認してください：



```
ssh <ip-address>

ls /mnt/labs

cat /mnt/labs/uploads/document.txt

echo 'EDITED once more by Azure VM.' >> /mnt/labs/uploads/document.txt

exit
```


ポータルで変更を確認してください。すべての接続されたクライアントは同じデータを見ます。

## ラボ

ファイル共有には保存できるデータの容量に上限があります。満杯になると、クライアントはデータを書き込む際にエラーが発生します。既存の共有の容量を増やすことはできますか？

Azureファイルは高速な固体ディスクを使用するプレミアム階層もサポートしています。100GBの容量でプレミアム共有を作成してください。プレミアム階層について何が異なりますか？

> 詰まったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

ラボRGを削除します：



```
az group delete -y -n labs-storage-files --no-wait
```
