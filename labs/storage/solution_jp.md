# ラボ解決策

## ポータルを使用する場合

ストレージアカウントを開きます：

- _ネットワーキング_ ブレードを選択します
- _選択した仮想ネットワークとIPアドレスから有効にする_ を選びます
- あなたのクライアントIPアドレスを追加します

## またはCLIを使用する場合：



```
# 公開アクセスをオフにする：
az storage account update -g labs-storage -n <sa-name> --default-action Deny

# あなたの公開IPアドレスを見つける（または https://www.whatsmyip.org にアクセスする）
curl ifconfig.me

# 既存のルールを確認する
az storage account network-rule list -g labs-storage --account-name <sa-name>

# あなたのIPアドレスを許可するルールを追加する：
az storage account network-rule add -g labs-storage --account-name <sa-name> --ip-address 213.18.157.115 #<public-ip-address>
```


## ダウンロードできることを確認する

あなたのマシンから：



```
curl -o download4.txt https://<sa-name>.blob.core.windows.net/drops/document.txt

cat download4.txt
```


> 文書のテキストが表示され、XMLエラー文字列ではないはずです

## VMがダウンロードできないことを確認する

VMの詳細を取得し、接続します：



```
az vm list -o table -g labs-storage --show-details

ssh <vm01-ip-address>
```


VMのターミナル内でファイルのダウンロードを試みます：



```
curl -o download5.txt https://<sa-name>.blob.core.windows.net/drops/document.txt

cat download5.txt
```


> XMLの _AuthorizationFailure_ メッセージが表示されます
