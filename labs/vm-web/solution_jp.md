# ラボ解決策

PIP を変更する前に、VM が割り当て解除されていることを確認します：



```
az vm deallocate -g labs-vm-web -n vm01
```


それが完了したら、PIP の _割り当て方法_ を変更して、動的アドレスの代わりに静的 IP アドレスを要求します：


```
az network public-ip update -g labs-vm-web -n vm01PublicIP --allocation-method Static
```


次に、PIP の詳細を確認すると、IP アドレスが表示されます。それは PIP を使用する任意の VM のアドレスであり、PIP を削除するまで変更されません。

> [演習](README_jp.md)に戻ります。
