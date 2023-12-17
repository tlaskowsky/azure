# ラボ解決策

JMESPath を使用して、タグに基づいて RG をフィルタリングすることができます：



```
az group list -o table --query "[?tags.courselabs == 'azure']" 
```


レスポンスにリソースの詳細の一部のみを含めるには、最後にフィールド名を追加します：



```
az group list -o table --query "[?tags.courselabs == 'azure'].name"
```


テーブル ヘッダーを失うために TSV 形式に切り替えます：



```
az group list -o tsv --query "[?tags.courselabs == 'azure'].name"
```

> これで、delete コマンドにフィードできるグループ名のリストができました。

これを実行する方法は、使用しているシェルによります。

_PowerShell では：_

```
az group list -o tsv --query "[?tags.courselabs == 'azure'].name" | foreach { az group delete -y -n $_ }
```


_Bash では：_



```
for rg in $(az group list -o tsv --query "[?tags.courselabs == 'azure'].name"); do az group delete -y -n ${rg}; done
```
