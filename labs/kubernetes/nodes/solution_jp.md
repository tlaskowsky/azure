# 実習の解決策

オブジェクトのラベルを確認する最も簡単な方法は、`get`コマンドに`show-labels`オプションを使用することです：



```
kubectl get nodes --show-labels
```


特定のラベル値を見るためには、ラベルを列として印刷できます：



```
kubectl get nodes --label-columns kubernetes.io/arch,kubernetes.io/os
```


または、JSONPathを使用してメタデータ内のラベルフィールドをクエリすることもできます：



```
kubectl get node <your-node> -o jsonpath='{.metadata.labels}'
```


あるいは、Goテンプレートを用いて特定の値をクエリすることができます：



```
kubectl get node <your-node> -o go-template=$'{{index .metadata.labels "kubernetes.io/arch"}}'

# またはPowerShellで：
kubectl get node <your-node> -o go-template=$'{{index .metadata.labels `"kubernetes.io/arch`"}}'
```


(JSONPathはラベルキーのスラッシュを好まない)

> [演習](README_jp.md)に戻る。
