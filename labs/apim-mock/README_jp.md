# APIマネジメント：新しいAPIのモック

新しいAPIは、APIアーキテクト、データオーナー、APIコンシューマー間の3者間で設計されることが多いです。このアプローチにより、ベストプラクティスに従い、利用可能なデータからコンシューマーが必要とする情報を提供するAPIを持つことができます。

設計から納品までの期間は長くなる可能性があるため、_モック_ を公開することが良いアイデアです。これは設計で合意されたすべての操作を持つ実際のAPIサービスですが、ダミーデータを返します。実際のAPIが利用可能になるまで、チームはモックに対してプログラムを作成できます。

このラボでは、APIマネジメントを使用してAPIを設計し、モックレスポンスを公開し、合意された仕様に準拠しているかをテストします。

## 参照

- [APIレスポンスのモック](https://learn.microsoft.com/en-us/azure/api-management/mock-api-responses?tabs=azure-portal)

## 新しいAPIの作成

[APIマネジメントラボ](/labs/apim/README_jp.md)から既存のAPIマネジメントサービスがあるはずです。ポータルでそれを参照し、新しいAPIを作成します：

- _手動定義されたHTTP API_ を選択
- 任意の名前とURLを入力
- APIMのURLサフィックスとして `newapi` を使用


_Definitions（定義）_ タブを開きます - これはAPIが取り扱うオブジェクトのタイプを定義する場所です。

このサンプルJSONから _Student（学生）_ という定義を作成します：



```
{
    "StudentId": 2315125,
    "FullName" : "Test One"
}
```


このサンプルJSONからもう一つの定義 _StudentDetail（学生詳細）_ を作成します：


```
{
    "StudentId": 2315125,
    "CompanyId": 124121,
    "FirstName" : "Test",
    "LastName" : "Two",
    "Courses" : [
        {
            "CourseCode": "AZDEVACAD",
            "Completed" : "22-11"
        },
        {
            "CourseCode": "K8SFUN",
            "Completed" : "21-01"
        }
    ]
}
```


最後に、この**ペイロード**を使用して _StudentArray（学生配列）_ という配列定義を作成します：


```
{
    "type": "array",
    "items": {
        "$ref": "#/definitions/Student"
    }
}
```


APIは、これらのリソース定義を使用して学生を管理します。

## モック操作の追加

API設計に学生のリスト表示、学生の作成、学生の詳細取得、学生の削除といった操作を追加します。

- _List Students（学生のリスト）_
    - URL `/students` からのGET
    - `200 OK` レスポンスを返す
        - `StudentArray` 定義の `application/json` 表現を含む

- _Create Student（学生の作成）_
    - URL `/students` へのPOST
    - リクエストペイロードを含む
        - `StudentDetail` 定義の `application/json` 表現
    - `201 Created` レスポンスを返す
        - `StudentDetail` 定義の `application/json` 表現を含む
        
- _Get Student（学生の取得）_
    - URL `/students/{studentId}` からのGET
    - `studentId` をテンプレートパラメータとして含む
    - `200 OK` レスポンスを返す
        - `StudentDetail` 定義の `application/json` 表現を含む
    - `404 Not found` レスポンスを返す
        - ペイロードなし

- _Delete Student（学生の削除）_
    - URL `/students/{studentId}` へのDELETE
    - `studentId` をテンプレートパラメータとして含む
    - `204 No Content` レスポンスを返す
        - ペイロードなし
    - `404 Not found` レスポンスを返す
        - ペイロードなし

各操作について：

- _インバウンド処理_ ポリシーを追加
- `mocked-response` ポリシーを使用
- 正しいレスポンスコードを選択

> 各操作をテストして、正しいデータタイプでモックレスポンスが得られるか確認します

## モックAPIの公開


新しいAPIを _Unlimited（無制限）_ 製品に追加し、その製品のサブスクリプションを作成します。

サブスクリプションキーを使用してcurlでAPIをテストします - 各操作に対してモックレスポンスが返されるはずです：


```
# これは学生の配列を返すはずです：
curl "https://<apim-name>.azure-api.net/newapi/students" -H "Ocp-Apim-Subscription-Key: <subscription-key>"

# これは学生の詳細を返すはずです：
curl "https://<apim-name>.azure-api.net/newapi/students/1234" -H "Ocp-Apim-Subscription-Key: <subscription-key>"
```


curlはREST APIの試金石です - コマンドラインから操作できれば、コンシューマーもコードで扱うことができます。

## Postmanで消費＆テスト

ただし、curlはあまりユーザーフレンドリーではありません。REST APIを扱うための優れた（無料の）ツールはPostmanです：

- [Postmanをインストール](https://www.postman.com/downloads/) *または*
- [Postmanオンラインを試す](https://web.postman.co/home)

PostmanはREST APIを扱うための最も人気のあるツールの一つです。リクエストを設定し、変数をパラメータ化することができるため、非常に柔軟です。

このリポジトリには、モックアウトしたAPIのコンシューマーの期待を含むPostman _コレクション_ があります。Postmanにそれをインポートし、モックにポイントして、すべての操作呼び出しを行うことができるはずです：

- コレクションファイル `labs/apim-mock/students.postman_collection.json` をインポート
- コレクションを開き、_Variables（変数）_ タブに移動：

![Postman コレクション変数](/img/postman-collection-variables.png)

APIMで作成したモックAPIの値を設定します：

- `baseUrl` は完全なURLです。例：`https://myapim.azure-api.net/newapi`
- `apiKey` はサブスクリプションキー - curlで使用したのと同じものです

_Save（保存）_ をクリックし、すべての操作を試してみてください - すべて期待通りのレスポンスコードとレスポンスを返すはずです。そうでない場合は、APIMでのAPI設計を確認する必要があります。

## ラボ

このAPI仕様はAPIMデザイナーで手動で作成されました。作成するのは簡単ですが、共有するのは簡単ではありません。API仕様をコンシューマーにどのように配布しますか？

> 詰まったら、[提案](suggestions_jp.md) を試してみてください

___

## クリーンアップ

**まだクリーンアップしないでください！**

1つのAPIMインスタンスで複数のAPIをホストできます。次の数回のラボで同じリソースを使用するため、削除して代わりを作成するのにまた1時間待つのではなく、そのままにしておきます。
