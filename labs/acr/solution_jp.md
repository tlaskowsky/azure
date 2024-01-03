# ラボの解決策

これを自動化したい場合、レジストリ内のすべてのリポジトリを一覧表示することから始める必要があります：



```
az acr repository list -n <acr-name>
```


次に、結果をループして各リポジトリのタグを一覧表示します。このコマンドは、タグがプッシュされた日付によって出力を並べ替えることができます。例えば：


```
az acr repository show-tags -n <acr-name> --repository labs-acr/simple-web --output tsv --orderby time_desc
```


次に、リストをイテレートし、最初の5つをスキップします - なぜならそれらは保持したい最新のものだからです。残りのものについては、完全なイメージ名を構築し、それを削除します：


```
az acr repository delete --name $acrName --image $imageName --yes --only-show-errors
```


こちらに PowerShell のサンプルスクリプトがあります: [prune-acr.ps1](./lab/prune-acr.ps1)。
