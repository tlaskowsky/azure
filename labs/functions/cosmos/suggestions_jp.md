# Labの提案

これは翻訳されたドキュメントをどのように使用するかに大きく依存します。同じコレクションに保存すると、翻訳を持つすべてのドキュメントを簡単にクエリできるため（新しいドキュメントは元のドキュメントのIDを保存するため）、コレクションを横断してクエリを実行する必要はありません。

トリガー内でドキュメントをフィルタリングして、翻訳が挿入されたときにトリガーが呼び出されないようにする方法はありません。

代替策として、翻訳を別のコンテナに保存する方法が考えられます。たとえば、`posts-es` という名前のコンテナです。これにより、トリガーが翻訳が挿入されたときに再び発生するのを防ぎますが、クエリがより難しく（および高価に）なる可能性があります。

そして、無限ループについて考えてみてください。翻訳されたフィールドを元のドキュメントに「追加」することを決定した場合、次のように見えるようにした場合：


```
{
    "id": "897",
    "message": "hello",
    "lang" : "en",
    "translatedMessage" : "hola",
    "translatedLang" : "es",
    "translatedTimestamp": 221112024412
}
```


あなたのロジックは翻訳されたメッセージが既に存在するかどうかを確認する必要があります。そうでない場合、新しいタイムスタンプ付きで翻訳を設定すると、繰り返しトリガーが発生し続けます...
