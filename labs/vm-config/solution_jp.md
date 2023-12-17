# ラボ解決策

Windows VMで：



```
az vm run-command invoke  --command-id IPConfig -g labs-vm-config --name dev01
```

そしてLinux VMで：



```
az vm run-command invoke  --command-id ifconfig -g labs-vm-config --name web01
```


出力は読みにくいかもしれませんが、内部IPアドレスを探すと、`10.0.0.x` の範囲にあるはずです。両方のマシンが同じサブネットにあるので、一緒に接続されているようです。

Windows VMからLinux VMへの ping でこれをテストできます：



```
az vm run-command invoke  -g labs-vm-config --name dev01 --command-id RunPowerShellScript --scripts "ping <linux-vm-ip>"
```


ポータルにアクセスすると、VMがどのように接続されているかがわかります。

> [演習](README_jp.md)に戻ります。
