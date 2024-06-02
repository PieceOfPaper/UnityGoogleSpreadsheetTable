# UnityGoogleSpreadsheetTable

## 1. 설치
1. Unity NuGet Package Manager 설치 (https://github.com/GlitchEnzo/NuGetForUnity)
2. Unity NuGet Package Manager로 `Google.Apis` `Google.Apis.Sheets.v4` 패키지 설치
3. UnityGoogleSpreadsheetTable.package 실행하여 패키지 설치

## 2. Google API 설정
https://console.cloud.google.com/apis/credentials
1. 사용자 인증 정보 만들기 - OAuth 클라이언트 ID
2. OAuth 클라이언트 ID JSON 파일 다운로드

## 3. 사용 가능한 자료형
### C#
 - string (String)
 - byte (Byte)
 - short (Int16)
 - int (Int32)
 - long (Int64)
 - float (Single)
 - double (Double)
### Unity
 - Vector2
 - Vector3
 - Vector4
 - Vector2Int
 - Vector3Int
 - Color
### Unity.Mathematics
 - int2
 - int3
 - int4
 - float2
 - float3
 - float4
### Unity.Collections
 - FixedString32Bytes (string32로도 입력 가능)
 - FixedString64Bytes (string64로도 입력 가능)
 - FixedString128Bytes (string128로도 입력 가능)
 - FixedString256Bytes (string256로도 입력 가능)
 - FixedString512Bytes (string512로도 입력 가능)
 - FixedString4096Bytes (string4096로도 입력 가능)
### Enum
 - enum:로 시작하는 타입 입력시, enum 타입을 검색하여 사용.
 - ex) enum:UnityEngine.RenderMode
### Array
 - array:로 시작하는 타입 입력시, 배열로 사용.
 - 사용 가능한 자료형: byte, short, int, long, decimal, float, double
 - ex) array:int
### Others
 - ColorCode (Hex코드로 입력, 코드는 UnityEngine.Color로 생성됨)
