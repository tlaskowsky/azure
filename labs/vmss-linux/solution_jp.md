# ラボの解決策

我々が行いたいプロセスは、VMSSのインスタンスの再イメージングです。Portalでインスタンスを選択して再イメージングするか、コマンドラインを使用できます。

すべてのVMを一括して再イメージングすることもできますが、最新のものは正しい状態にあります。代わりに、元のVMのインスタンスIDを見つけて個別に再イメージングする必要があります。



```
# この場合、元のVMのインスタンスIDは0、2、および3でした。
az vmss reimage --instance-id 0 -g labs-vmss-linux -n vmss-web01

az vmss reimage --instance-id 2 -g labs-vmss-linux -n vmss-web01

az vmss reimage --instance-id 3 -g labs-vmss-linux -n vmss-web01
```


Portalでは、インスタンスの状態が「更新中」に変わり、それが行われている間にインスタンスはロード バランサーから取り外されるため、再イメージングされたインスタンスがオンラインに戻るまで、新しいVMからの応答のみが表示されるはずです。

> [演習に戻る](README_jp.md)
