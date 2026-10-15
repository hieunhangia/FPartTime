# HƯỚNG DẪN CHUẨN THIẾT KẾ VÀ KIỂM TRA REQUEST DTO TRONG ASP.NET CORE (.NET 8+)

---

## 0. PHẠM VI ÁP DỤNG VÀ GIẢ ĐỊNH

- Áp dụng cho **Controller-based API** (`[ApiController]`) nhận dữ liệu qua **JSON body** (`System.Text.Json`). Với `multipart/form-data`, query string và route data, dữ liệu được gán bởi **Model Binder** thay vì `System.Text.Json`, nhưng các quy tắc về `T?` + `[Required]` bên dưới vẫn áp dụng được.
- **Minimal API**: trước .NET 10 không tự chạy DataAnnotations; từ .NET 10 cần bật validation (`AddValidation()`). Tài liệu này không bao quát Minimal API.
- Ngôn ngữ C# 12 trở lên (collection expression `= []`). Nếu dự án dùng C# cũ hơn, thay `= []` bằng `= new();` cho `List<T>` / `Dictionary<K,V>` (lưu ý: quy tắc cấm `= new();` ở Quy tắc 5 chỉ áp dụng cho **object con tự định nghĩa**, không áp dụng cho collection).
- Giả định project bật `<Nullable>enable</Nullable>` (xem mục 1.3).

---

## 1. NGUYÊN LÝ CỐT LÕI (CORE PRINCIPLES)

### 1.1. Luồng xử lý

Một HTTP Request đi qua 2 bước trước khi vào Controller/Service:

1. **Model Binding / JSON Deserialization (`System.Text.Json`):** Đọc chuỗi JSON và khởi tạo instance DTO trong C#.
2. **Model Validation (`DataAnnotations`):** Chạy các Attribute như `[Required]`, `[Range]`, `[MaxLength]` để điền lỗi vào `ModelState`.

### 1.2. Cạm bẫy lớn nhất: `[Required]` chỉ kiểm tra `null`

`RequiredAttribute.IsValid(object? value)` hoạt động như sau:

- `value is null` → trả về `false` (báo lỗi).
- `value is string` và `AllowEmptyStrings = false` → trả về `false` nếu chuỗi rỗng hoặc chỉ gồm khoảng trắng.
- Mọi trường hợp khác → **trả về `true` (hợp lệ)**.

**Hệ quả 1 – Value Type non-nullable:** `DateOnly`, `Guid`, `int`, `enum`... **không bao giờ mang giá trị `null`**. Khi client không gửi trường đó, C# gán giá trị mặc định (`0`, `0001-01-01`, `Guid.Empty`...). Lúc này `[Required]` thấy giá trị khác `null` nên **bị vô hiệu hóa (bypass)**.

**Hệ quả 2 – Collection:** `[Required]` chỉ kiểm tra `list != null`, một danh sách rỗng `[]` vẫn hợp lệ.

### 1.3. Các attribute độ dài tự bỏ qua `null`

`[MinLength]`, `[MaxLength]` và `[Length]` (.NET 8+) đều **trả về hợp lệ khi value là `null`** (mã nguồn .NET ghi rõ: dùng `[Required]` để đảm bảo giá trị không null). Vì vậy:

- Một collection bắt buộc **phải có cả `[Required]` lẫn `[Length]`/`[MinLength]`**.
- Khởi tạo `= []` **không** ngăn `System.Text.Json` gán `null` khi client gửi tường minh `"items": null` (STJ mặc định không kiểm tra nullable annotation). Nếu NRT bật và property khai báo non-nullable, implicit `[Required]` (mục 1.4) sẽ chặn giá trị này nhưng với thông báo tiếng Anh mặc định; `null` chỉ lọt hoàn toàn khi NRT tắt, khi bật cờ `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes`, hoặc khi property khai báo `?`.
- Vì vậy collection bắt buộc nên khai báo `?` và gắn `[Required(ErrorMessage = "...")]` tường minh, để có thông báo tùy chỉnh và phân biệt được "thiếu" với "rỗng".

### 1.4. Nullable Reference Types (NRT) và implicit `[Required]`

Khi project bật `<Nullable>enable</Nullable>`, MVC **tự động thêm `[Required]` ngầm** cho mọi property reference type **non-nullable** (trừ khi cấu hình `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`). Điều này dẫn đến:

- Property optional kiểu reference **bắt buộc phải khai báo `?`** (`string?`, `AddressDto?`), nếu không sẽ bị coi là bắt buộc.
- Thông báo của implicit `[Required]` là thông báo mặc định tiếng Anh, vì vậy trường bắt buộc vẫn phải gắn `[Required(ErrorMessage = "...")]` tường minh để có thông báo tùy chỉnh.
- Implicit `[Required]` dùng `AllowEmptyStrings = true`, nên chuỗi rỗng `""` vẫn hợp lệ nếu không gắn `[Required]` tường minh.
- Implicit `[Required]` **không áp dụng cho Value Type**, nên các quy tắc `T?` ở dưới vẫn luôn cần thiết.

### 1.5. Enum nhận được giá trị ngoài phạm vi

`System.Text.Json` mặc định chấp nhận số nguyên bất kỳ cho enum (ví dụ `"gender": 99`). Giá trị này pass `[Required]` và pass deserialization. Cần thêm `[EnumDataType(typeof(TEnum))]` (hoặc kiểm tra `Enum.IsDefined` ở Service), hoặc cấu hình `JsonStringEnumConverter` với `allowIntegerValues: false`. `[EnumDataType]` có xử lý riêng cho enum gắn `[Flags]` (hợp lệ nếu là tổ hợp các cờ đã định nghĩa); lưu ý giá trị `0` bị coi là không hợp lệ nếu enum không có member bằng 0.

---

## 2. BẢNG PHÂN LOẠI KIỂU DỮ LIỆU (TYPE CLASSIFICATION)

Trước khi viết bất kỳ thuộc tính nào trong Request DTO, **bắt buộc phải xác định nó thuộc nhóm nào**:

| Nhóm | Kiểu dữ liệu trong C# | Giá trị mặc định khi Client KHÔNG gửi | Có thể mang giá trị `null`? |
| :--- | :--- | :--- | :--- |
| **Value Type (Số)** | `int`, `long`, `short`, `byte`, `float`, `double`, `decimal` | `0` (`0m`) | **Không** |
| **Value Type (Logic)** | `bool` | `false` | **Không** |
| **Value Type (Thời gian)** | `DateOnly`, `TimeOnly`, `DateTime`, `DateTimeOffset`, `TimeSpan` | `0001-01-01`, `00:00:00` | **Không** |
| **Value Type (Định danh)** | `Guid` | `Guid.Empty` (`00000000-0000-0000-0000-000000000000`) | **Không** |
| **Value Type (Enum)** | Bất kỳ `enum` nào (ví dụ: `Gender`, `JobStatus`) | Giá trị `0` của enum (có thể trùng một member, hoặc **không trùng member nào** nếu enum không định nghĩa member bằng 0) | **Không** |
| **Reference Type (Chuỗi)** | `string` | `null` (hoặc `""` nếu khởi tạo `= string.Empty;`) | **Có** |
| **Reference Type (Object tự định nghĩa)** | `class`, `record class` (ví dụ: `AddressRequestDto`) | `null` (hoặc instance rỗng nếu khởi tạo `= new();`) | **Có** |
| **Reference Type (Tập hợp)** | `List<T>`, `T[]`, `ICollection<T>`, `HashSet<T>` | `null` (hoặc rỗng nếu khởi tạo `= [];`) | **Có** |
| **Reference Type (Từ điển)** | `Dictionary<TKey, TValue>` | `null` (hoặc rỗng nếu khởi tạo `= [];`) | **Có** |
| **Reference Type (File upload)** | `IFormFile` | `null` | **Có** |

> 💡 **Cách nhận biết nhanh trong IDE:** Hover chuột vào tên kiểu dữ liệu:
> - Thấy từ khóa **`struct`**, **`record struct`** hoặc **`enum`** → **Value Type**.
> - Thấy từ khóa **`class`**, **`record`** (record class) hoặc **`interface`** → **Reference Type**.

---

### BẢNG TRA CỨU NHANH TỔNG HỢP (MASTER CHEAT SHEET)

| Kiểu dữ liệu | Nhóm | Cú pháp khi BẮT BUỘC | Cú pháp khi TÙY CHỌN | Lưu ý sống còn |
| :--- | :--- | :--- | :--- | :--- |
| `int`, `decimal`, `double`, `bool` | Value Type | `[Required(ErrorMessage = "...")]`<br>`public decimal? Amount { get; set; }` | `public decimal? Amount { get; set; }` | Phải có `?`, nếu không client không gửi sẽ nhận `0`/`false`. |
| `DateOnly`, `DateTime`, `TimeOnly` | Value Type | `[Required(ErrorMessage = "...")]`<br>`public DateOnly? ExpiryAt { get; set; }` | `public DateOnly? ExpiryAt { get; set; }` | Struct non-nullable nhận `0001-01-01`, bypass `[Required]`. |
| `Guid` | Value Type | `[Required(ErrorMessage = "...")]`<br>`public Guid? Id { get; set; }` | `public Guid? Id { get; set; }` | Non-nullable `Guid` nhận `Guid.Empty`, bypass `[Required]`. |
| `enum` | Value Type | `[Required(ErrorMessage = "...")]`<br>`[EnumDataType(typeof(Gender))]`<br>`public Gender? Gender { get; set; }` | `[EnumDataType(typeof(Gender))]`<br>`public Gender? Gender { get; set; }` | Non-nullable enum âm thầm nhận giá trị `0`. Luôn thêm `[EnumDataType]` để chặn số ngoài phạm vi. |
| `string` | Reference Type | `[Required(ErrorMessage = "...")]`<br>`public string Title { get; set; } = string.Empty;` | `public string? Note { get; set; }` | Khởi tạo `= string.Empty;` để hết cảnh báo CS8618. Không dùng C# `required`. |
| **Object tự định nghĩa** (`AddressDto`...) | Reference Type | `[Required(ErrorMessage = "...")]`<br>`public AddressDto? Address { get; set; }` | `public AddressDto? Address { get; set; }` | **TUYỆT ĐỐI KHÔNG** khởi tạo `= new();` (xem Quy tắc 5). |
| `List<T>`, `T[]` | Reference Type | `[Required(ErrorMessage = "...")]`<br>`[Length(1, Max, ErrorMessage = "...")]`<br>`public List<T>? Items { get; set; }` | `[MaxLength(Max, ErrorMessage = "...")]`<br>`public List<T> Items { get; set; } = [];` | `[Required]` không bắt được `[]`; `[Length]`/`[MinLength]` không bắt được `null`. Bắt buộc dùng **cả hai**. |
| `Dictionary<K, V>` | Reference Type | `[Required(ErrorMessage = "...")]`<br>`[MinLength(1, ErrorMessage = "...")]`<br>`public Dictionary<K, V>? Meta { get; set; }` | `public Dictionary<K, V> Meta { get; set; } = [];` | Cùng nguyên tắc với `List<T>`. |
| `IFormFile` | Reference Type | `[Required(ErrorMessage = "...")]`<br>`public IFormFile? File { get; set; }` | `public IFormFile? File { get; set; }` | Nhận qua `multipart/form-data`. |

---

## 3. BỘ QUY TẮC THIẾT KẾ (DESIGN RULES BY SCENARIO)

### Quy tắc 1: Value Types (`DateOnly`, `Guid`, `int`, `decimal`, `TimeOnly`, `enum`, `bool`)

#### Trường hợp 1.1: BẮT BUỘC (Required)

👉 **LUÔN LUÔN khai báo dạng Nullable (`T?`) kết hợp `[Required(ErrorMessage = "...")]`**

```csharp
// ĐÚNG:
[Required(ErrorMessage = "Hạn nhận hồ sơ là bắt buộc.")]
public DateOnly? ExpiryAt { get; set; }

[Required(ErrorMessage = "Ngành nghề là bắt buộc.")]
public Guid? IndustryId { get; set; }

[Required(ErrorMessage = "Đơn vị tính lương là bắt buộc.")]
[EnumDataType(typeof(JobSalaryTimeUnit), ErrorMessage = "Đơn vị tính lương không hợp lệ.")]
public JobSalaryTimeUnit? TimeUnit { get; set; }

[Required(ErrorMessage = "Số tiền lương là bắt buộc.")]
[Range(typeof(decimal), "1000", "500000000", ErrorMessage = "Số tiền phải từ 1,000 đến 500,000,000 VNĐ.")]
public decimal? Amount { get; set; }

[Required(ErrorMessage = "Ngày trong tuần là bắt buộc.")]
[EnumDataType(typeof(DayOfWeek), ErrorMessage = "Ngày trong tuần không hợp lệ.")]
public DayOfWeek? DayOfWeek { get; set; }
```

> **Lưu ý với `[Range]` trên `decimal`:** overload `Range(int, int)` ép giá trị về `int` bằng `Convert.ToInt32`, dẫn đến làm tròn ở biên (999,6 bị tròn thành 1000 nên lọt qua) và gây `OverflowException` (không được `RangeAttribute` bắt, có thể thành lỗi 500) khi giá trị vượt `int.MaxValue`. Với `decimal`, dùng `Range(typeof(decimal), "min", "max")`.

❌ **SAI (Cấm tuyệt đối):**

```csharp
// NGUY HIỂM: Không gửi trường → nhận 0001-01-01 → [Required] bị qua mặt!
[Required(ErrorMessage = "Hạn nhận hồ sơ là bắt buộc.")]
public DateOnly ExpiryAt { get; set; }

// NGUY HIỂM: Không gửi trường → nhận Guid.Empty → [Required] bị qua mặt!
[Required(ErrorMessage = "Ngành nghề là bắt buộc.")]
public Guid IndustryId { get; set; }

// NGUY HIỂM: Không gửi trường → tự gán DayOfWeek.Sunday (0) mà không báo lỗi!
[Required(ErrorMessage = "Ngày trong tuần là bắt buộc.")]
public DayOfWeek DayOfWeek { get; set; }
```

#### Trường hợp 1.2: TÙY CHỌN (Optional)

👉 **Khai báo dạng Nullable (`T?`) và KHÔNG có attribute `[Required]`**

```csharp
// ĐÚNG:
[EnumDataType(typeof(Gender), ErrorMessage = "Giới tính không hợp lệ.")]
public Gender? GenderRequirement { get; set; }

[Range(15, 100, ErrorMessage = "Tuổi tối thiểu phải từ 15 đến 100.")]
public int? MinAgeRequirement { get; set; }
```

#### Trường hợp 1.3: Giá trị mặc định được gửi tường minh

`T?` + `[Required]` chỉ bắt trường hợp client **không gửi** hoặc gửi `null`. Nếu client gửi tường minh `Guid.Empty` (`"00000000-0000-0000-0000-000000000000"`) hoặc `"0001-01-01"`, giá trị vẫn hợp lệ với `[Required]`. Khi các giá trị này không có ý nghĩa nghiệp vụ, dùng custom attribute hoặc kiểm tra ở Service:

```csharp
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null || (value is Guid g && g != Guid.Empty);
}

[Required(ErrorMessage = "Ngành nghề là bắt buộc.")]
[NotEmptyGuid(ErrorMessage = "Ngành nghề không hợp lệ.")]
public Guid? IndustryId { get; set; }
```

---

### Quy tắc 2: Chuỗi ký tự (`string`)

#### Trường hợp 2.1: BẮT BUỘC (Required)

👉 **Khai báo non-nullable `string` + khởi tạo `= string.Empty;` + gắn `[Required(ErrorMessage = "...")]`**

```csharp
// ĐÚNG:
[Required(ErrorMessage = "Tiêu đề công việc là bắt buộc.")]
[MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
public string Title { get; set; } = string.Empty;
```

*Lợi ích:*
1. Không còn cảnh báo biên dịch `CS8618: Non-nullable property is uninitialized`.
2. Khi client không gửi trường, gửi `null`, gửi `""` hoặc `"   "`, `[Required]` đều kích hoạt `ErrorMessage` tùy chỉnh.
3. **KHÔNG** dùng từ khóa `required` của C# 11 trên Request DTO. `System.Text.Json` sẽ ném `JsonException` (`"JSON deserialization for type ... was missing required properties..."`) ngay ở bước deserialize, trước khi DataAnnotations chạy, làm mất `ErrorMessage` tùy chỉnh.

#### Trường hợp 2.2: TÙY CHỌN (Optional)

👉 **Khai báo dạng `string?` và KHÔNG gắn `[Required]`**

```csharp
// ĐÚNG:
[MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
public string? Note { get; set; }
```

---

### Quy tắc 3: Danh sách / Collection (`List<T>`, `T[]`)

Có **hai** điểm yếu cần xử lý đồng thời:
- `[Required]` chỉ kiểm tra `!= null`, danh sách rỗng `[]` vẫn pass.
- `[Length]`, `[MinLength]`, `[MaxLength]` đều **bỏ qua `null`**, nên `null` vẫn pass.

#### Trường hợp 3.1: BẮT BUỘC có ít nhất 1 phần tử

👉 **Khai báo Nullable (`List<T>?`) và kết hợp `[Required]` + `[Length(min, max)]` (.NET 8+) hoặc `[MinLength]`/`[MaxLength]`**

```csharp
// ĐÚNG (.NET 8+):
[Required(ErrorMessage = "Danh sách lịch làm việc là bắt buộc.")]
[Length(1, 10, ErrorMessage = "Số lượng lịch làm việc phải từ 1 đến 10.")]
public List<JobScheduleRequestDto>? JobSchedules { get; set; }

// ĐÚNG (trước .NET 8, chưa có [Length]):
[Required(ErrorMessage = "Danh sách lịch làm việc là bắt buộc.")]
[MinLength(1, ErrorMessage = "Phải có ít nhất 1 lịch làm việc.")]
[MaxLength(10, ErrorMessage = "Tối đa 10 lịch làm việc.")]
public List<JobScheduleRequestDto>? JobSchedules { get; set; }
```

❌ **SAI:**

```csharp
// Thiếu [Required] tường minh → client gửi "jobSchedules": null chỉ bị implicit [Required] bắt (khi NRT bật) với thông báo tiếng Anh mặc định;
// khi NRT tắt, null vẫn pass [Length].
[Length(1, 10, ErrorMessage = "...")]
public List<JobScheduleRequestDto> JobSchedules { get; set; } = [];

// Thiếu [Length]/[MinLength] → client gửi [] vẫn pass [Required].
[Required(ErrorMessage = "...")]
public List<JobScheduleRequestDto> JobSchedules { get; set; } = [];
```

> Lưu ý: nếu collection chứa các object DTO con, validator sẽ tự đệ quy vào từng phần tử không-null; phần tử `null` trong list cần được xử lý riêng (ví dụ kiểm tra ở Service).

#### Trường hợp 3.2: TÙY CHỌN (Có thể rỗng)

👉 **Khởi tạo sẵn `= [];` và có thể thêm `[MaxLength]` để chặn spam số lượng**

```csharp
// ĐÚNG:
[MaxLength(5, ErrorMessage = "Tối đa 5 kỹ năng bổ sung.")]
public List<string> AdditionalSkills { get; set; } = [];
```

> Với NRT bật, nếu client gửi tường minh `null` cho property non-nullable này, implicit `[Required]` sẽ báo lỗi. Nếu muốn chấp nhận `null`, khai báo `List<string>?` và xử lý `null` ở Service (ví dụ `dto.AdditionalSkills ?? []`).

---

### Quy tắc 4: File Upload (`IFormFile`)

Nhận qua `multipart/form-data`:

#### Trường hợp 4.1: BẮT BUỘC

👉 **Khai báo `IFormFile?` kết hợp `[Required(ErrorMessage = "...")]`**

```csharp
// ĐÚNG:
[Required(ErrorMessage = "Ảnh CCCD mặt trước là bắt buộc.")]
public IFormFile? CCCDFrontImage { get; set; }
```

#### Trường hợp 4.2: TÙY CHỌN

👉 **Khai báo `IFormFile?` và KHÔNG có `[Required]`**

```csharp
// ĐÚNG:
public IFormFile? AvatarImage { get; set; }
```

> DTO chứa `IFormFile` phải được nhận bằng `[FromForm]` trên tham số của action, vì `[ApiController]` mặc định suy luận tham số kiểu complex type là `[FromBody]`.

---

### Quy tắc 5: Object tự định nghĩa (Custom Classes, Nested DTO, Records)

Áp dụng cho các `class` / `record class` do lập trình viên định nghĩa, dùng làm DTO con lồng nhau (ví dụ: `AddressRequestDto`, `CompanyProfileDto`).

#### 5.1. Object con BẮT BUỘC

👉 **Khai báo Nullable (`T?`) kết hợp `[Required(ErrorMessage = "...")]`, KHÔNG khởi tạo `= new();`**

```csharp
public class RegisterCompanyRequestDto
{
    [Required(ErrorMessage = "Tên công ty là bắt buộc.")]
    public string CompanyName { get; set; } = string.Empty;

    // ĐÚNG: Nullable + [Required]
    [Required(ErrorMessage = "Thông tin địa chỉ trụ sở là bắt buộc.")]
    public AddressRequestDto? HeadquarterAddress { get; set; }
}

public class AddressRequestDto
{
    [Required(ErrorMessage = "Tên đường là bắt buộc.")]
    public string Street { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xã/Phường là bắt buộc.")]
    public string CommuneCode { get; set; } = string.Empty;
}
```

❌ **Cạm bẫy nếu khởi tạo `= new();`:**

```csharp
// SAI:
[Required(ErrorMessage = "Thông tin địa chỉ trụ sở là bắt buộc.")]
public AddressRequestDto HeadquarterAddress { get; set; } = new();
```

*Tại sao sai?*
- Nếu client **không gửi** trường `headquarterAddress`, C# đã khởi tạo sẵn một instance rỗng, nên `[Required]` của property cha luôn pass.
- Validator sau đó đi vào bên trong instance rỗng này và báo hàng loạt lỗi con (*"Tên đường là bắt buộc"*, *"Xã/Phường là bắt buộc"*).
- Người dùng nhận lỗi chi tiết của từng trường con thay vì thông báo cấp cao: *"Thông tin địa chỉ trụ sở là bắt buộc"*.

#### 5.2. Object con TÙY CHỌN nhưng các trường bên trong là Bắt buộc

👉 **Khai báo Nullable (`T?`), KHÔNG gắn `[Required]`, và TUYỆT ĐỐI KHÔNG khởi tạo `= new();`**

```csharp
public class CreateCandidateProfileDto
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    public string FullName { get; set; } = string.Empty;

    // TÙY CHỌN: có thể điền hoặc không điền địa chỉ tạm trú
    public AddressRequestDto? TemporaryAddress { get; set; }
}
```

*Cơ chế hoạt động:*
- **Client KHÔNG gửi `temporaryAddress` (hoặc gửi `null`):** Property mang giá trị `null`. Validator thấy `null` và không có `[Required]` nên **bỏ qua**, không validate các trường bên trong. Request **hợp lệ**.
- **Client CÓ gửi `"temporaryAddress": { "note": "nhà thuê" }` (thiếu `street`):** Property được khởi tạo (khác `null`), validator đệ quy vào bên trong và báo lỗi *"Tên đường là bắt buộc"*.
- ⚠️ Nếu viết `public AddressRequestDto TemporaryAddress { get; set; } = new();`, object này luôn khác `null`, biến một object tùy chọn thành **luôn luôn bị validate bắt buộc**.

#### 5.3. Record và Constructor trong DTO

1. `System.Text.Json` dùng constructor public không tham số nếu type có. Constructor có tham số được dùng tự động khi nó là constructor public duy nhất (trường hợp positional record). Khi có nhiều constructor và cần chỉ định rõ constructor nào được dùng, gắn `[JsonConstructor]`.
2. Với positional record, **MVC lấy validation từ tham số của primary constructor**, vì vậy DataAnnotations phải đặt trực tiếp trên tham số, **không** dùng tiền tố `property:`. Nếu đặt attribute validation trên property của record, MVC ném `InvalidOperationException` ("Record type '...' has validation metadata defined on property '...' that will be ignored...") khi validate.

   ```csharp
   // ĐÚNG:
   public record AddressDto(
       [Required(ErrorMessage = "Tên đường là bắt buộc.")] string Street,
       [Required(ErrorMessage = "Thành phố là bắt buộc.")] string City
   );

   // SAI: MVC ném InvalidOperationException khi validate.
   public record AddressDto(
       [property: Required(ErrorMessage = "Tên đường là bắt buộc.")] string Street
   );
   ```

3. Các quy tắc `T?` / `= string.Empty` / không `= new();` ở trên áp dụng tương tự cho record, nhưng cú pháp positional khiến việc đặt giá trị mặc định và attribute kém trực quan hơn.

👉 **Khuyến nghị chuẩn:** Dùng `public class SomeRequestDto { public ... { get; set; } }` cho Request DTO để đồng nhất cú pháp, tránh bẫy attribute trên record ở trên, dễ áp dụng attribute và dễ review bằng checklist.

#### 5.4. Dictionary (`Dictionary<string, string>`)

```csharp
// Bắt buộc có ít nhất 1 cặp key-value (cần cả [Required] lẫn [MinLength]):
[Required(ErrorMessage = "Thuộc tính mở rộng là bắt buộc.")]
[MinLength(1, ErrorMessage = "Phải có ít nhất 1 thuộc tính mở rộng.")]
public Dictionary<string, string>? Attributes { get; set; }

// Tùy chọn (cho phép rỗng):
public Dictionary<string, string> Attributes { get; set; } = [];
```

---

## 4. XỬ LÝ TRONG SERVICE VÀ MAPPER KHI DTO LÀ NULLABLE

### 4.1. Tại Service Validation

Mặc dù `[ApiController]` đã chặn hầu hết lỗi ở tầng Controller, Service vẫn nên kiểm tra an toàn (đặc biệt khi method được gọi nội bộ, từ job nền hoặc Unit Test):

```csharp
private async Task ValidateAsync(CreateUpdateJobRequestDto dto)
{
    // Kiểm tra an toàn trước khi truy cập .Value
    if (!dto.ExpiryAt.HasValue)
    {
        throw new BadRequestException("Hạn nhận hồ sơ là bắt buộc.");
    }

    if (!dto.IndustryId.HasValue)
    {
        throw new BadRequestException("Ngành nghề là bắt buộc.");
    }

    if (dto.IndustryId.Value == Guid.Empty)
    {
        throw new BadRequestException("Ngành nghề không hợp lệ.");
    }

    if (dto.JobSchedules is null || dto.JobSchedules.Count == 0)
    {
        throw new BadRequestException("Phải có ít nhất 1 lịch làm việc.");
    }

    // businessTimeZone: múi giờ nghiệp vụ của hệ thống (ví dụ UTC+7 cho Việt Nam)
    var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, businessTimeZone));
    if (dto.ExpiryAt.Value <= today)
    {
        throw new BadRequestException("Hạn nhận hồ sơ phải lớn hơn ngày hiện tại.");
    }

    if (!await dbContext.Industries.AnyAsync(i => i.Id == dto.IndustryId.Value))
    {
        throw new BadRequestException("Ngành nghề không tồn tại.");
    }
}
```

### 4.2. Tại Mapper (Riok.Mapperly / AutoMapper / Mapster)

- Khi map từ DTO sang Entity với source `DateOnly?` và target `DateOnly` (hoặc `required DateOnly`), các thư viện mapping thường chuyển `null` thành `default(T)` **một cách im lặng** thay vì ném lỗi.
- Nghĩa là nếu một giá trị `null` lọt qua tầng validate, Entity sẽ nhận `0001-01-01` / `Guid.Empty` / `0`, tái hiện đúng vấn đề ban đầu mà không có bất kỳ cảnh báo nào.
- **Mapper không phải là lớp bảo vệ.** Việc đảm bảo giá trị khác `null` phải hoàn tất ở tầng DataAnnotations và Service Validation **trước** khi gọi mapper.
- Có thể cấu hình mapper để ném lỗi khi gặp `null` ở các trường bắt buộc, hoặc viết map thủ công `source.ExpiryAt!.Value` sau khi đã validate.

### 4.3. Lỗi parse JSON

Lỗi định dạng phát sinh ở bước deserialize (ví dụ ngày sai định dạng, gửi `null` cho Value Type non-nullable, chuỗi không phải enum hợp lệ) do `System.Text.Json` ném ra, nên thông báo là tiếng Anh mặc định và không đi qua `ErrorMessage` của DataAnnotations. Nếu cần thông báo tiếng Việt cho các lỗi này, xử lý tập trung ở `ApiBehaviorOptions.InvalidModelStateResponseFactory` hoặc dùng `JsonConverter` tùy chỉnh.

---

## 5. ĐÁNH ĐỔI VỀ OPENAPI VÀ CLIENT GENERATOR

Việc dùng `T?` + `[Required]` cho Value Type khiến schema OpenAPI vừa nằm trong danh sách `required` vừa mang dấu hiệu `nullable`. Các client được sinh tự động (Kiota, NSwag, openapi-typescript...) có thể cho ra kiểu `T | null` cho trường bắt buộc, buộc phía client phải xử lý thêm `null`.

Đây là đánh đổi chấp nhận được để đổi lấy việc validate đúng và có thông báo lỗi tùy chỉnh. Nếu cần client có kiểu chặt hơn, xử lý ở tầng cấu hình OpenAPI (schema transformer / filter để bỏ `nullable` cho các trường có `[Required]`) thay vì đổi DTO về non-nullable.

---

## 6. BẢNG TỔNG KẾT ANTI-PATTERNS & CÁCH SỬA

| ❌ Code Vi Phạm (Anti-Pattern) | ⚠️ Nguy cơ / Lỗi xảy ra | ✅ Code Chuẩn Xác |
| :--- | :--- | :--- |
| `[Required] public DateOnly ExpiryAt { get; set; }` | Client không gửi → nhận `0001-01-01`, pass `[Required]`. Gửi `null` → lỗi parser tiếng Anh. | `[Required(ErrorMessage = "...")] public DateOnly? ExpiryAt { get; set; }` |
| `[Required] public Guid JobId { get; set; }` | Client không gửi → nhận `Guid.Empty`, pass `[Required]`. | `[Required(ErrorMessage = "...")] public Guid? JobId { get; set; }` |
| `[Required] public DayOfWeek Day { get; set; }` | Client không gửi → tự nhận `DayOfWeek.Sunday (0)`. | `[Required(ErrorMessage = "...")] [EnumDataType(typeof(DayOfWeek))] public DayOfWeek? Day { get; set; }` |
| `public Gender? Gender { get; set; }` (không có `[EnumDataType]`) | Client gửi `99` vẫn deserialize thành công, giá trị không tồn tại lọt vào hệ thống. | `[EnumDataType(typeof(Gender), ErrorMessage = "...")] public Gender? Gender { get; set; }` |
| `[Required] public required string Reason { get; set; }` | C# `required` làm `System.Text.Json` ném `JsonException` trước khi DataAnnotations chạy, mất `ErrorMessage` tùy chỉnh. | `[Required(ErrorMessage = "...")] public string Reason { get; set; } = string.Empty;` |
| `public string Title { get; set; }` (không gán `= string.Empty`) | Cảnh báo biên dịch `CS8618`. | `[Required(ErrorMessage = "...")] public string Title { get; set; } = string.Empty;` |
| `[Required] public List<ItemDto> Items { get; set; } = [];` | Client gửi `items: []` hoặc không gửi → vẫn pass `[Required]` vì `[] != null`. | `[Required(ErrorMessage = "...")] [Length(1, Max, ErrorMessage = "...")] public List<ItemDto>? Items { get; set; }` |
| `[Length(1, Max)] public List<ItemDto> Items { get; set; } = [];` | Client gửi `items: null` → property thành `null`, `[Length]` bỏ qua `null`. Khi NRT bật, implicit `[Required]` chặn nhưng với thông báo tiếng Anh mặc định; khi NRT tắt, `null` pass hoàn toàn. | `[Required(ErrorMessage = "...")] [Length(1, Max, ErrorMessage = "...")] public List<ItemDto>? Items { get; set; }` |
| `[Required] public decimal Amount { get; set; }` | Client không gửi → nhận `0m`, pass `[Required]`. | `[Required(ErrorMessage = "...")] [Range(typeof(decimal), "1", "Max")] public decimal? Amount { get; set; }` |
| `[Range(1, 1000)] public decimal? Amount { get; set; }` | Giá trị bị ép về `int`: làm tròn ở biên; giá trị lớn hơn `int.MaxValue` gây `OverflowException` không được bắt, có thể thành lỗi 500. | `[Range(typeof(decimal), "1", "1000")] public decimal? Amount { get; set; }` |
| `public record AddressDto([property: Required] string Street);` | MVC ném `InvalidOperationException` vì validation đặt trên property của record. | `public record AddressDto([Required(ErrorMessage = "...")] string Street);` hoặc dùng `class`. |
| `public AddressDto Address { get; set; } = new();` | Khởi tạo sẵn instance rỗng → nếu client không gửi trường cha, validator vẫn đi vào trong báo lỗi hàng loạt trường con, hoặc biến object tùy chọn thành luôn bị validate bắt buộc. | Bắt buộc: `[Required] public AddressDto? Address { get; set; }`<br>Tùy chọn: `public AddressDto? Address { get; set; }` (không bao giờ gán `= new()`). |
| `public AddressDto Address { get; set; }` (NRT bật, không `?`) | MVC tự thêm implicit `[Required]` với thông báo mặc định tiếng Anh; object tùy chọn trở thành bắt buộc. | Bắt buộc: `[Required(ErrorMessage = "...")] public AddressDto? Address { get; set; }`<br>Tùy chọn: `public AddressDto? Address { get; set; }` |

---

## 7. CHECKLIST TỰ KIỂM TRA CHO AI & DEVELOPER (AUDIT CHECKLIST)

Mỗi khi sinh hoặc review một file Request DTO, duyệt qua checklist này cho **từng property**:

- [ ] **1. Xác định type:** Thuộc tính là Value Type (struct, enum, số, ngày giờ, Guid, bool) hay Reference Type (string, collection, class)?
- [ ] **2. Value Type bắt buộc:** Đã có `?` (`T?`) và `[Required(ErrorMessage = "...")]` chưa?
- [ ] **3. Value Type tùy chọn:** Đã có `?` và không có `[Required]` chưa?
- [ ] **4. Enum:** Đã có `[EnumDataType(typeof(...))]` (hoặc cơ chế chặn giá trị ngoài phạm vi tương đương) chưa?
- [ ] **5. `string` bắt buộc:** Đã khởi tạo `= string.Empty;`, có `[Required]`, và **không** dùng từ khóa C# `required` chưa?
- [ ] **6. `string` tùy chọn:** Đã khai báo `string?` chưa?
- [ ] **7. Collection bắt buộc:** Đã khai báo Nullable và dùng **cả** `[Required]` **lẫn** `[Length(1, ...)]`/`[MinLength(1)]` chưa?
- [ ] **8. Collection tùy chọn:** Đã khởi tạo `= [];` (hoặc khai báo `?` nếu chấp nhận `null`) và có `[MaxLength]` chặn số lượng chưa?
- [ ] **9. Object con (Custom Class / Nested DTO):** Đã khai báo Nullable (`T?`) và **TUYỆT ĐỐI KHÔNG** khởi tạo `= new();` chưa? (Bắt buộc thì thêm `[Required]`, tùy chọn thì không thêm.)
- [ ] **10. NRT:** Mọi property reference type tùy chọn đã có `?` để tránh implicit `[Required]` chưa?
- [ ] **11. Thông báo lỗi:** Mọi `ErrorMessage` đã được viết thân thiện với người dùng (tiếng Việt có dấu) chưa? (Lỗi parse JSON xem mục 4.3.)
- [ ] **12. Tầng Service:** Logic nghiệp vụ đã truy cập an toàn bằng `.HasValue` / `.Value` / kiểm tra `null` collection chưa?
- [ ] **13. Mapper:** Đã đảm bảo không có giá trị `null` nào của trường bắt buộc được mapper chuyển thành `default(T)` một cách im lặng chưa?
- [ ] **14. Range:** Với `decimal`, đã dùng `[Range(typeof(decimal), "min", "max")]` thay vì `Range(int, int)` chưa?
- [ ] **15. Giá trị mặc định tường minh:** Với `Guid`/ngày bắt buộc, đã chặn `Guid.Empty` / `0001-01-01` (custom attribute hoặc Service) khi chúng không hợp lệ chưa?
- [ ] **16. Record:** Nếu dùng positional record, attribute validation đã đặt trên tham số constructor (không dùng `property:`) chưa?
- [ ] **17. IFormFile:** DTO chứa `IFormFile` đã được nhận bằng `[FromForm]` chưa?
