# ラボ解決策

## ポータルを使用する

_ストレージブラウザ_を開いてテーブルに移動します。

_詳細フィルタ_をクリックし、_Level_ プロパティでフィルタリングできます：



```
Level eq 'Error'
```


## ODataを使用する

新しいテーブルのためにSASトークンが必要です：

```
$expiry=$(Get-Date -Date (Get-Date).AddHours(1) -UFormat +%Y-%m-%dT%H:%MZ)

az storage table generate-sas -n FulfilmentLogs --permissions r --expiry $expiry -o tsv --account-name <sa-name>
```


ポータルと同じクエリ `Level eq 'Error'` を使用してフィルタしますが、URLに入れる必要があるため、[エンコード](https://www.w3schools.com/html/html_urlencode.asp)する必要があります。そのため、実際のURL内のクエリ文字列は次のようになります：


```
FulfilmentLogs()?$filter=Level%20eq%20%27Error%27
```


あなた自身のドメインとSASトークンを含む、完全なクエリは次のようになります：


```
curl -H 'Accept: application/json;odata=nometadata' "https://labsstoragetablees.table.core.windows.net/FulfilmentLogs()?$filter=Level%20eq%20%27Error%27&se=2022-10-27T19%3A51Z&sp=r&sv=2019-02-02&tn=FulfilmentLogs&sig=EdZElUPinN2RDnbjiNrzZNfm49LLE/F6st0dJj5bLjQ%3D"
```
