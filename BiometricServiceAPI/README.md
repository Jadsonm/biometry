# Biometric API

API que se comunica com um dispositivo biométrico local nitgen, perfeito para integração com aplicações web.

## Compilando

- Requer que as bibliotecas do SDK eNBioBSP estejam instaladas no sistema.
- .NET 7 ou superior
- Se você deseja apenas consumir a API, não é necessário compilar o projeto; basta baixar a versão mais recente da API na página de Lançamentos/Releases, executar o instalador e executar a API localmente

# Mapa da API

O prefixo é: `http://localhost:5000/apiservice/`  
Você pode alterar a porta em appsettings.json se precisar em caso de conflito.

#### GET: `capture-hash/`

Ativa o dispositivo biométrico para capturar sua impressão digital, caso tudo corra bem imagens da captura atual são salvas localmente no diretório `%temp%/fingers-registered` e é retornado:  
`200 | OK`

```json
{
    "fingers-registered": 1,
    "template": "AAAAAZCXZDSfe34t4f//...",  <------- fingerprint hash
    "fingers-id": [ 1, 6 ], <------- 1 to 5 - right thumb to right pinky: 6 to 10 - left thumb to left pinky
    "quality-FIR": 100, <------- quality value of the fingerprint data with a scale of 0 to 100
    "success": true
}
```

qualquer outra coisa:  
`400 | BAD REQUEST`

```json
{
  "message": "Error on Capture: {nitgen error code}",
  "success": false
}
```

Você pode passar um parâmetro opcional `img` para retornar a imagem da digital. As opções disponíveis são:

- `false`: Retorno padrão, sem a imagem.
- `true`: Retorna a imagem em base64.

Exemplo de uso e retorno:
`/capture-hash?img=true`
`200 | OK`

```json
{
    "fingers-registered": 1,
    "template": "AAAAAZCXZDSfe34t4f//...",  <------- fingerprint hash
    "fingers-id": [ 1, 2 ], <------- 1 to 5 - right thumb to right pinky: 6 to 10 - left thumb to left pinky
    "images": ["base64string1", "base64string2", "..."],  <------- array of base64 encoded images (1 to 5 - right thumb to right pinky: 6 to 10 - left thumb to left pinky)
    "quality-FIR": 100, <------- quality value of the fingerprint data with a scale of 0 to 100
    "success": true
}
```

---

#### GET: `capture-for-verify/`

Ativa o dispositivo biométrico para capturar sua impressão digital para verificação, caso tudo corra bem, retorna:  
`200 | OK`

```json
{
    "template": "AAAAAZCXZDSfe34t4f//...",  <------- fingerprint hash
    "image": "base64",                      <------- fingerprint image
    "success": true
}
```

qualquer outra coisa:  
`400 | BAD REQUEST`

```json
{
  "message": "Error on Capture: {nitgen error code}",
  "success": false
}
```

Você pode passar um parâmetro opcional `window` para definir o estilo da janela de captura. As opções disponíveis são:

- `0` (POPUP): Janela de captura padrão.
- `1` (INVISIBLE): Captura invisível.
- `65536` (NO_FPIMG): Sem imagem de impressão digital.
- `131072` (TOPMOST): Janela sempre no topo.
- `262144` (NO_WELCOME): Sem mensagem de boas-vindas.
- `524288` (NO_TOPMOST): Janela não fica no topo.

Exemplo de uso:
`/capture-for-verify?window=1`

---

#### POST: `match-one-on-one/`

Recebe um template e ativa o dispositivo biométrico para comparar:

##### conteúdo da requisição POST:

```json
{
  "template": "AAAAAZCXZDSfe34t4f//..."
}
```

caso o procedimento de verificação corra bem, retorna:
`200 | OK`

```json
{
    "message": "Fingerprint matches / Fingerprint doesnt match",
    "success": true/false
}
```

qualquer outra coisa:
`400 | BAD REQUEST`

```json
{
  "message": "Timeout / Error on Verify: {nitgen error code}",
  "success": false
}
```

Você pode passar um parâmetro opcional `img` para retornar a imagem da digital. As opções disponíveis são:

- `false`: Retorno padrão, sem a imagem.
- `true`: Retorna a imagem em base64.

Exemplo de uso e retorno:
`/match-one-on-one?img=true`
`200 | OK`

```json
{
    "message": "Fingerprint matches / Fingerprint doesnt match",
    "image": "base64",  <------- fingerprint image
    "success": true/false
}
```

##### Compatibilidade com templates do SÊNIOR RH

O endpoint `match-one-on-one` aceita dois formatos de template automaticamente:

| Formato | Origem | Codificação | Detecção |
|---|---|---|---|
| **NITGEN TextFIR** | Gerado por esta API (`capture-hash`) | Base64 customizado com `*` | Contém `*` |
| **FIM01_HV** | Gerado pelo SÊNIOR RH (SAMReaders) | Base64 padrão com `+` | Não contém `*` |

**Por que os formatos são diferentes?**

O SÊNIOR utiliza o SDK Java do NITGEN (`NBioBSPJNI`) para capturar digitais. Internamente, o SAMReaders chama `ExportFIR(type=7)`, que exporta o template em formato binário compacto **FIM01_HV** — diferente do formato texto (`TextFIR`) gerado pelo SDK .NET. Esse binário é então codificado com `java.util.Base64.getEncoder()` (Base64 padrão) antes de ser enviado ao servidor.

**O que a API faz com um template FIM01_HV:**

```
Template SÊNIOR (Base64 padrão)
        ↓
Convert.FromBase64String() → bytes brutos FIM01_HV
        ↓
NBioAPI.Export.FDxToNBioBSP(bytes, MINCONV_TYPE_FIM01_HV, ...) → HFIR handle
        ↓
NBioAPI.Verify(hFIR, ...) → captura ao vivo pelo leitor → compara
```

`FDxToNBioBSP` é o método do SDK .NET que faz o inverso do `ExportFIR(type=7)` do Java, convertendo os bytes FIM01_HV de volta para um handle interno que o SDK consegue usar para verificação.

---

#### GET: `identification/`

Captura sua impressão digital e faz uma busca no índice (1:N) a partir do banco de dados em memória, caso tudo corra bem:  
`200 | OK`

```json
{
    "message": "Fingerprint match found / Fingerprint match not found",
    "id": id_number,     <------ returns 0 in case its not found
    "success": true/false
}
```

qualquer outra coisa:  
`400 | BAD REQUEST`

```json
{
  "message": "Error on Capture: {nitgen error code}",
  "success": false
}
```

Caso encontre problemas com a validação da impressão digital sendo muito rigorosa ou muito permissível, é possível passar um parâmetro opcional `secuLevel` para reduzir ou aumentar o nível de segurança da validação entre 1 (mínimo) e 9 (máximo), o padrão é 5.

`/identification?secuLevel=9`

---

#### POST: `load-to-memory/`

Recebe um **array** de templates com ID para carregar na memória do index search:

##### POST REQUEST content:

```json
[
    {
        "id": id_number,        <------ e.g: 1, 2, 3  or 4235, 654646, 23423
        "template": "AAAAAZCXZDSfe34t4f//..."
    },
    {
        "id": id_number,
        "template": "AAAAAZCXZDSfe3ff454t4f//..."
    },
    ...
]
```

caso o procedimento de verificação corra bem, retorna:  
`200 | OK`

```json
{
  "message": "Templates loaded to memory",
  "success": true
}
```

qualquer outra coisa:  
`400 | BAD REQUEST`

```json
{
  "message": "Error on AddFIR: {nitgen error code}",
  "success": false
}
```

---

#### GET: `delete-all-from-memory/`

Exclui todos os dados armazenados na memória para uso no index search, caso tudo corra bem, retorna:  
`200 | OK`

```json
{
  "message": "All templates deleted from memory",
  "success": true
}
```

---

#### GET : `total-in-memory`

Retorna a quantidade de templates armazenados na memória:
`200 | OK`

```json
{
	"total": 0,  <------ total templates
	"success": true
}
```

---

#### GET : `device-unique-id`

Retorna o ID único do dispositivo biométrico:
`200 | OK`

```json
{
	"serial": "FF-FF-FF-FF-FF-FF-FF-FF",  <------ device ID
	"success": true
}
```

---

#### POST : `join-templates`

Recebe dois ou mais templates e retorna um template único com a informação de todos os dedos registrados:

##### POST REQUEST content:

```json
[
    {
        "template": "AAAAAZCXZDSfe34t4f//..."
    },
    {
        "template": "AAAAAZCXZDSfe3ff454t4f//..."
    },
    ...
]
```

caso o procedimento de verificação corra bem, retorna:
`200 | OK`

```json
{
	"template": "AAAAAZCXZDSfe34t4f//...",  <------ combined hash
	"message": "Templates joined successfully",
	"success": true
}
```

qualquer outra coisa:  
`400 | BAD REQUEST`

```json
{
  "message": "Error creating template: {nitgen error code}",
  "success": false
}
```
