# 実験解決策

マルチアーキテクチャイメージタグとOSを設定して `az` コマンドを使用できます：



```
az container create -g labs-aci --name simple-web-win --image courselabs/simple-web:6.0 --ports 80 --os Windows --dns-name-label <aci-win-dns>
```


OSを指定しない場合、ACIはLinuxコンテナを作成します。そのため、ACIにそれがWindowsであると伝えずにWindowsイメージタグを使用すると、エラーが発生します：


```
# このコマンドは、コンテナのOSがイメージのOSと一致しないというエラーを返します
az container create -g labs-aci --name simple-web-win2 --image courselabs/simple-web:6.0-windows-amd64 --ports 80 --dns-name-label <aci-win-dns2>
```


したがって、OSも設定する必要があります：


```
az container create -g labs-aci --name simple-web-win2 --image courselabs/simple-web:6.0-windows-amd64 --ports 80 --os Windows --dns-name-label <aci-win-dns2>
```


ACIにはARM64のサポートがありませんが、コンテナが作成されるときにイメージCPUが検証されるわけではありません。ACIはあなたがコンテナを実行させます：


```
az container create -g labs-aci --name simple-web-arm --image courselabs/simple-web:6.0-linux-arm64 --ports 80 --dns-name-label <aci-arm-dns>
```


しかし、コンテナは起動するとすぐに終了します。ログをチェックすると、実行フォーマットに関するメッセージが表示されます - これは、コンパイルされたバイナリとランタイムのCPUが一致しないことを示しています：


```
az container logs -g labs-aci -n simple-web-arm
```


が返すメッセージ：

*standard_init_linux.go:228: exec user process caused: exec format error*
