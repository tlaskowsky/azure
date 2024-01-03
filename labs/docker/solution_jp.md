# ラボ解決策

appsettings.json ファイルには以下の設定があります：



```
{
  "App": {
    "Environment": "DEV"
  }
}
```


この設定はコード内で `App:Environment` キーを使用してアクセスします。これを環境変数の名前としても使用できますが、すべてのプラットフォームが名前にコロンを含むことを好まないため、`App__Environment` としても使用できます。

これは [Dockerfile](/src/simple-web/Dockerfile) にも記述されており、以下の方法でイメージ内の設定を1つのコンテナに対して上書きします：


```
docker run -d -p 8084:80 -e App__Environment=PROD simple-web 
```


> 異なるポートを使用する必要があります。なぜなら、1つのポートには1つのコンテナしかリスニングできないからです。

http://localhost:8084/ にアクセスすると、更新された値が表示されます。
