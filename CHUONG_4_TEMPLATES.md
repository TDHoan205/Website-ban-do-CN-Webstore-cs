# CHƯƠNG 4: TỔ CHỨC THỰC THI VÀ ĐẢM BẢO CHẤT LƯỢNG

---

## 4.1 Lập kế hoạch quản lý dự án

### 4.1.1 Tổ chức nhân sự

| STT | Vai trò | Số lượng | Trách nhiệm |
|-----|---------|----------|-------------|
| 1 | Trưởng nhóm | 1 | Quản lý tiến độ, điều phối công việc |
| 2 | Lập trình viên Backend | 1 | Phát triển API, business logic |
| 3 | Lập trình viên Frontend | 1 | Thiết kế giao diện |
| 4 | Chuyên viên AI | 1 | Phát triển chatbot AI |
| 5 | Tester | 1 | Kiểm thử phần mềm |

### 4.1.2 Kế hoạch thực hiện

| Giai đoạn | Thời gian | Công việc chính |
|-----------|-----------|-----------------|
| Giai đoạn 1 | Tuần 1-2 | Phân tích yêu cầu, thiết kế database |
| Giai đoạn 2 | Tuần 3-6 | Phát triển Backend (Models, Controllers, Services) |
| Giai đoạn 3 | Tuần 7-10 | Phát triển Frontend (Views, CSS, JS) |
| Giai đoạn 4 | Tuần 11-13 | Tích hợp AI chatbot, RAG engine |
| Giai đoạn 5 | Tuần 14-15 | Tích hợp thanh toán (VNPay, Sepay, COD) |
| Giai đoạn 6 | Tuần 16-18 | Kiểm thử, sửa lỗi, tối ưu |
| Giai đoạn 7 | Tuần 19-20 | Triển khai, đào tạo, bàn giao |

### 4.1.3 Chiến lược cài đặt

**Môi trường triển khai:**
- Development: Máy local để phát triển
- Staging: Server test để kiểm thử trước khi release
- Production: Server thật để sử dụng thực tế

**Quy trình triển khai:**
```
Code → Git → Build → Test → Deploy Staging → UAT → Deploy Production
```

### 4.1.4 Chiến lược bảo trì

| Loại bảo trì | Mô tả | Tần suất |
|-------------|-------|----------|
| Corrective | Sửa lỗi phát sinh | Khi có lỗi |
| Adaptive | Cập nhật bảo mật | Hàng tháng |
| Perfective | Tối ưu hiệu suất | Hàng quý |
| Preventive | Refactor code | Hàng quý |

### 4.1.5 Chiến lược nâng cấp

| Thành phần | Phiên bản hiện tại | Phiên bản target |
|------------|-------------------|------------------|
| .NET Core | 8.0 | 9.0 |
| Entity Framework | 8.0.10 | Cập nhật minor |
| Gemini API | 1.5-flash | 2.0 |

---

## 4.2 Môi trường lập trình và mô tả module

### 4.2.1 Công nghệ sử dụng

| Thành phần | Công nghệ | Phiên bản |
|------------|-----------|-----------|
| Backend | ASP.NET Core MVC | 8.0 |
| Database | SQL Server | 2019+ |
| ORM | Entity Framework Core | 8.0.10 |
| Frontend | Razor Views, Bootstrap | 5.x |
| JavaScript | jQuery | 3.x |
| AI | Google Gemini API | 1.5 Flash |
| Thanh toán | VNPay, Sepay, COD | - |

### 4.2.2 Các module chính của hệ thống

| Module | Mô tả | Controller |
|--------|-------|------------|
| Authentication | Đăng nhập, đăng ký, phân quyền (Admin, Customer) | AuthController |
| Shop/Catalog | Trang chủ, danh sách sản phẩm, tìm kiếm, lọc | ShopController |
| Shopping Cart | Thêm, sửa, xóa sản phẩm trong giỏ hàng | CartItemsController |
| Checkout/Payment | Thanh toán COD, VNPay, Sepay | ShopController |
| Order Management | Quản lý đơn hàng (Admin xem tất cả, Customer xem của mình) | OrdersController |
| Product Management | CRUD sản phẩm (Admin) | ProductsController |
| AI Chatbot | Trợ lý ảo AI với RAG | ChatController |
| Statistics | Thống kê doanh thu (Admin) | StatisticsController |

### 4.2.3 Mô tả chi tiết các module

#### 4.2.3.1 Module Authentication (Đăng nhập/Đăng ký)

**Chức năng:** Đăng nhập, đăng ký, đăng xuất, phân quyền (Admin, Customer)

**File chính:** Controllers/AuthController.cs, Models/Account.cs

---

#### 4.2.3.2 Module Shop/Catalog (Cửa hàng)

**Chức năng:** Trang chủ, danh sách sản phẩm, tìm kiếm, lọc theo danh mục/giá, chi tiết sản phẩm với biến thể

**File chính:** Controllers/ShopController.cs, Services/ProductService.cs

---

#### 4.2.3.3 Module Shopping Cart (Giỏ hàng)

**Chức năng:** Thêm/sửa/xóa sản phẩm, tính tổng tiền, lưu trong Session

**File chính:** Controllers/CartItemsController.cs, Services/CartService.cs

---

#### 4.2.3.4 Module Checkout/Payment (Thanh toán)

**Chức năng:** Thanh toán COD, VNPay, Sepay, tạo đơn hàng

**File chính:** Controllers/ShopController.cs, Services/OrderService.cs

---

#### 4.2.3.5 Module Order Management (Quản lý đơn hàng)

**Chức năng:** Xem/sửa/hủy đơn hàng, cập nhật trạng thái

**Trạng thái:** Pending → Processing → Shipped → Delivered / Cancelled

**File chính:** Controllers/OrdersController.cs

---

#### 4.2.3.6 Module Product Management (Quản lý sản phẩm)

**Chức năng:** CRUD sản phẩm, quản lý biến thể, upload hình ảnh, quản lý danh mục/nhà cung cấp

**File chính:** Controllers/ProductsController.cs, CategoriesController.cs, SuppliersController.cs

---

#### 4.2.3.7 Module AI Chatbot (Trợ lý ảo)

**Chức năng:** Chat AI 24/7, nhận diện ý định, RAG tìm kiếm tri thức, FAQ tự động, gợi ý sản phẩm

**Kiến trúc AI:**
```
User → IntentDetection → RAGEngine → AIAgent → GeminiService → Response
```

**File chính:** Services/AI/GeminiService.cs, AIAgentService.cs, RAGEngineService.cs, ChatController.cs

---

#### 4.2.3.7 Module Statistics (Thống kê)

**Chức năng:** Dashboard, thống kê doanh thu, top sản phẩm bán chạy

**File chính:** Controllers/StatisticsController.cs

### 4.2.4 Cách chạy ứng dụng

1. Mở project trong Visual Studio 2022
2. Cài đặt SQL Server, tạo database `TechShopWebsite1`
3. Cập nhật connection string trong `appsettings.json`
4. Chạy lệnh `Update-Database` để tạo bảng
5. Nhấn F5 để chạy

**Tài khoản mặc định:**

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@gmail.com | Admin@123 |
| Customer | customer@gmail.com | Customer@123 |

---

## 4.3 Kế hoạch kiểm thử phần mềm

### 4.3.1 Các loại kiểm thử

| Loại kiểm thử | Mục tiêu | Phạm vi |
|--------------|----------|---------|
| Unit Test | Kiểm thử từng hàm riêng lẻ | Services, Utilities |
| Integration Test | Kiểm thử tương tác giữa các module | Controllers + Services |
| UI Test | Kiểm thử chức năng trên giao diện | Views, User flows |
| Regression Test | Đảm bảo code mới không phá vỡ chức năng cũ | Toàn bộ hệ thống |

### 4.3.2 Công cụ kiểm thử

| Loại | Công cụ |
|------|---------|
| Unit Test | xUnit / NUnit |
| API Test | Postman |
| UI Test | Manual testing |
| Bug Tracking | GitHub Issues |

---

## 4.4 Kiểm thử và đánh giá

### 4.4.1 Danh sách test case

#### A. Module Đăng nhập / Đăng ký (Authentication)

| STT | Tên Test Case | Đối tượng | Bước thực hiện | Kết quả mong đợi |
|-----|---------------|-----------|-----------------|------------------|
| TC01 | Đăng nhập admin đúng | Admin | Mở trang login → nhập admin@gmail.com / Admin@123 → Đăng nhập | Vào được trang Dashboard admin, hiển thị menu quản lý |
| TC02 | Đăng nhập khách hàng đúng | Khách hàng | Mở trang login → nhập customer@gmail.com / Customer@123 → Đăng nhập | Vào được trang cửa hàng, thấy giỏ hàng |
| TC03 | Đăng nhập sai mật khẩu | Mọi đối tượng | Nhập đúng username, sai password | Hiển thị thông báo lỗi, không vào được hệ thống |
| TC04 | Đăng nhập sai email | Mọi đối tượng | Nhập email chưa đăng ký, password bất kỳ | Hiển thị thông báo lỗi "Tài khoản không tồn tại" |
| TC05 | Đăng ký tài khoản mới | Khách hàng | Nhấn "Đăng ký" → nhập email mới, mật khẩu → Đăng ký | Tạo tài khoản thành công, chuyển về trang đăng nhập |
| TC06 | Đăng ký email đã tồn tại | Khách hàng | Nhấn "Đăng ký" → nhập admin@gmail.com → Đăng ký | Hiển thị lỗi "Email đã được sử dụng" |
| TC07 | Đăng xuất | Mọi đối tượng | Đã đăng nhập → nhấn nút Đăng xuất | Xóa session, chuyển về trang đăng nhập |

#### B. Module Cửa hàng / Sản phẩm (Shop/Catalog)

| STT | Tên Test Case | Đối tượng | Bước thực hiện | Kết quả mong đợi |
|-----|---------------|-----------|-----------------|------------------|
| TC08 | Xem trang chủ | Khách hàng | Truy cập trang chủ | Hiển thị sản phẩm nổi bật, danh mục |
| TC09 | Xem danh sách sản phẩm | Khách hàng | Vào Shop → Danh sách sản phẩm | Hiển thị grid sản phẩm với phân trang |
| TC10 | Tìm kiếm sản phẩm theo tên | Khách hàng | Nhập "iPhone" vào thanh tìm kiếm | Hiển thị các sản phẩm iPhone |
| TC11 | Lọc sản phẩm theo danh mục | Khách hàng | Chọn danh mục "Điện thoại" | Chỉ hiển thị điện thoại |
| TC12 | Lọc sản phẩm theo khoảng giá | Khách hàng | Chọn khoảng giá 10tr - 20tr | Hiển thị sản phẩm trong khoảng giá |
| TC13 | Sắp xếp sản phẩm theo giá | Khách hàng | Chọn sắp xếp "Giá: Thấp đến Cao" | Sản phẩm sắp xếp đúng thứ tự |
| TC14 | Xem chi tiết sản phẩm | Khách hàng | Click vào một sản phẩm | Hiển thị thông tin chi tiết, hình ảnh, giá |
| TC15 | Chọn biến thể sản phẩm | Khách hàng | Chi tiết sản phẩm → chọn màu "Đen", dung lượng "256GB" | Cập nhật giá và hình ảnh theo biến thể |

#### C. Module Giỏ hàng (Shopping Cart)

| STT | Tên Test Case | Đối tượng | Bước thực hiện | Kết quả mong đợi |
|-----|---------------|-----------|-----------------|------------------|
| TC16 | Thêm sản phẩm vào giỏ | Khách hàng | Xem sản phẩm → nhấn "Thêm vào giỏ" | Sản phẩm xuất hiện trong giỏ hàng |
| TC17 | Thêm cùng sản phẩm 2 lần | Khách hàng | Thêm cùng sản phẩm 2 lần | Số lượng sản phẩm = 2 |
| TC18 | Cập nhật số lượng trong giỏ | Khách hàng | Giỏ hàng → thay đổi số lượng lên 5 → Lưu | Tổng tiền được cập nhật theo số lượng mới |
| TC19 | Xóa sản phẩm khỏi giỏ | Khách hàng | Giỏ hàng → nhấn nút xóa 1 sản phẩm | Sản phẩm biến mất khỏi giỏ |
| TC20 | Xóa toàn bộ giỏ hàng | Khách hàng | Nhấn "Xóa giỏ hàng" | Giỏ hàng trống, hiển thị thông báo |
| TC21 | Tính tổng tiền chính xác | Khách hàng | Thêm nhiều sản phẩm vào giỏ | Tổng tiền = tổng(giá × số lượng) đúng |

#### D. Module Thanh toán / Đặt hàng (Checkout/Payment)

| STT | Tên Test Case | Đối tượng | Bước thực hiện | Kết quả mong đợi |
|-----|---------------|-----------|-----------------|------------------|
| TC22 | Đặt hàng COD thành công | Khách hàng | Giỏ hàng → Đặt hàng → chọn COD → điền thông tin → Xác nhận | Tạo đơn hàng, hiển thị trang thành công |
| TC23 | Đặt hàng với thông tin trống | Khách hàng | Checkout → bỏ trống số điện thoại → Xác nhận | Hiển thị lỗi validation, không tạo đơn |
| TC24 | Thanh toán qua VNPay | Khách hàng | Checkout → chọn VNPay → Xác nhận | Chuyển hướng sang trang thanh toán VNPay |
| TC25 | Thanh toán qua Sepay | Khách hàng | Checkout → chọn Sepay → Xác nhận | Hiển thị thông tin chuyển khoản ngân hàng |
| TC26 | Checkout khi giỏ trống | Khách hàng | Giỏ hàng trống → vào trang checkout | Chuyển về trang giỏ hàng |
| TC27 | Callback VNPay thành công | Khách hàng | Quay về từ VNPay với mã thành công | Cập nhật trạng thái đơn hàng = "Đã thanh toán" |
| TC28 | Callback VNPay thất bại | Khách hàng | Quay về từ VNPay với mã lỗi | Giữ nguyên trạng thái, hiển thị thông báo lỗi |

#### E. Module Quản lý Đơn hàng (Order Management)

| STT | Tên Test Case | Đối tượng | Bước thực hiện | Kết quả mong đợi |
|-----|---------------|-----------|-----------------|------------------|
| TC29 | Xem danh sách đơn hàng | Admin | Đăng nhập admin → Quản lý đơn hàng | Hiển thị danh sách tất cả đơn hàng |
| TC30 | Xem chi tiết đơn hàng | Admin | Click vào một đơn hàng | Hiển thị thông tin chi tiết, danh sách sản phẩm |
| TC31 | Cập nhật trạng thái đơn hàng | Admin | Sửa đơn → chọn "Đang giao hàng" → Lưu | Trạng thái đơn được cập nhật |
| TC32 | Hủy đơn hàng | Admin | Chọn đơn → nhấn "Hủy đơn" | Trạng thái = "Đã hủy" |
| TC33 | Khách hàng xem lịch sử đơn | Khách hàng | Đăng nhập → Tài khoản → Lịch sử đơn hàng | Hiển thị đơn hàng của khách hàng đó |

#### F. Module Quản lý Sản phẩm (Admin)

| STT | Tên Test Case | Đối tượng | Bước thực hiện | Kết quả mong đợi |
|-----|---------------|-----------|-----------------|------------------|
| TC34 | Xem danh sách sản phẩm | Admin | Đăng nhập admin → Quản lý sản phẩm | Hiển thị danh sách sản phẩm |
| TC35 | Thêm sản phẩm mới | Admin | Nhấn "Thêm sản phẩm" → nhập thông tin đầy đủ → Lưu | Sản phẩm mới xuất hiện trong danh sách |
| TC36 | Sửa thông tin sản phẩm | Admin | Chọn sản phẩm → Sửa → thay đổi giá → Lưu | Thông tin sản phẩm được cập nhật |
| TC37 | Xóa sản phẩm | Admin | Chọn sản phẩm → Xóa | Sản phẩm không hiển thị trong danh sách |
| TC38 | Thêm biến thể sản phẩm | Admin | Sản phẩm → Thêm biến thể → chọn màu, dung lượng | Biến thể mới được tạo |

#### G. Module AI Chatbot (Trợ lý ảo)

| STT | Tên Test Case | Đối tượng | Bước thực hiện | Kết quả mong đợi |
|-----|---------------|-----------|-----------------|------------------|
| TC39 | Chat hỏi về sản phẩm | Khách hàng | Nhắn "Có iPhone nào không?" | Bot trả lời có sản phẩm iPhone liên quan |
| TC40 | Chat hỏi giá sản phẩm | Khách hàng | Nhắn "Giá iPhone 15 bao nhiêu?" | Bot trả lời đúng giá sản phẩm |
| TC41 | Chat hỏi về chính sách đổi trả | Khách hàng | Nhắn "Chính sách đổi trả?" | Bot trả lời thông tin đổi trả |
| TC42 | Chat không rõ ý định | Khách hàng | Nhắn "asdfghjkl" | Bot phản hồi mặc định, gợi ý câu hỏi |
| TC43 | Chat lịch sử cuộc trò chuyện | Khách hàng | Gửi nhiều tin nhắn liên tiếp | Bot nhớ ngữ cảnh từ các tin nhắn trước |

#### H. Module Thống kê (Statistics)

| STT | Tên Test Case | Đối tượng | Bước thực hiện | Kết quả mong đợi |
|-----|---------------|-----------|-----------------|------------------|
| TC44 | Xem dashboard thống kê | Admin | Quản lý → Thống kê | Hiển thị biểu đồ doanh thu, đơn hàng |
| TC45 | Lọc thống kê theo ngày | Admin | Chọn khoảng ngày | Biểu đồ cập nhật theo khoảng ngày |
| TC46 | Top sản phẩm bán chạy | Admin | Xem thống kê | Hiển thị danh sách sản phẩm bán chạy |

---

### 4.4.2 Kết quả kiểm thử

#### Tổng quan kết quả

| Module | Số test case | Pass | Fail | Tỷ lệ pass |
|--------|-------------|------|------|------------|
| Authentication | 7 | 7 | 0 | 100% |
| Shop/Catalog | 8 | 8 | 0 | 100% |
| Shopping Cart | 6 | 6 | 0 | 100% |
| Checkout/Payment | 7 | 7 | 0 | 100% |
| Order Management | 5 | 5 | 0 | 100% |
| Product Management | 5 | 5 | 0 | 100% |
| AI Chatbot | 5 | 5 | 0 | 100% |
| Statistics | 3 | 3 | 0 | 100% |
| **Tổng cộng** | **46** | **46** | **0** | **100%** |

#### Chi tiết kết quả kiểm thử một số module quan trọng

**Module Authentication:**

| STT | Test Case | Kết quả | Ghi chú |
|-----|-----------|---------|---------|
| TC01 | Đăng nhập admin đúng | **PASS** | Vào Dashboard admin thành công |
| TC02 | Đăng nhập khách hàng đúng | **PASS** | Vào trang cửa hàng thành công |
| TC03 | Đăng nhập sai mật khẩu | **PASS** | Hiển thị "Sai mật khẩu" |
| TC04 | Đăng nhập sai email | **PASS** | Hiển thị "Tài khoản không tồn tại" |
| TC05 | Đăng ký tài khoản mới | **PASS** | Tạo tài khoản thành công |
| TC06 | Đăng ký email trùng | **PASS** | Hiển thị "Email đã được sử dụng" |
| TC07 | Đăng xuất | **PASS** | Chuyển về trang login |

*(Minh họa: Ảnh chụp màn hình đăng nhập thành công / thất bại)*

**Module Shopping Cart & Checkout:**

| STT | Test Case | Kết quả | Ghi chú |
|-----|-----------|---------|---------|
| TC16 | Thêm sản phẩm vào giỏ | **PASS** | Sản phẩm xuất hiện với số lượng = 1 |
| TC17 | Thêm cùng sản phẩm 2 lần | **PASS** | Số lượng tự động tăng lên 2 |
| TC18 | Cập nhật số lượng | **PASS** | Tổng tiền cập nhật đúng |
| TC22 | Đặt hàng COD | **PASS** | Tạo đơn, hiển thị mã đơn hàng |
| TC24 | Thanh toán VNPay | **PASS** | Chuyển hướng sang VNPay thành công |

*(Minh họa: Ảnh chụp giỏ hàng, trang checkout, thông báo đặt hàng thành công)*

**Module AI Chatbot:**

| STT | Test Case | Kết quả | Ghi chú |
|-----|-----------|---------|---------|
| TC39 | Hỏi về sản phẩm | **PASS** | Bot trả lời với thông tin sản phẩm phù hợp |
| TC40 | Hỏi giá sản phẩm | **PASS** | Bot trả đúng giá |
| TC41 | Hỏi chính sách đổi trả | **PASS** | Bot trả lời đúng FAQ |
| TC42 | Tin nhắn không rõ ý | **PASS** | Bot gợi ý các câu hỏi phổ biến |
| TC43 | Chat có ngữ cảnh | **PASS** | Bot nhớ được câu hỏi trước đó |

*(Minh họa: Ảnh chụp màn hình chat với AI chatbot)*

#### Bảng tổng hợp lỗi (nếu có)

| STT | Mã lỗi | Mô tả lỗi | Module | Mức độ | Trạng thái |
|-----|--------|-----------|--------|--------|------------|
| - | - | Không có lỗi | - | - | - |

**Giải thích mức độ lỗi:**
- **Cao**: Lỗi nghiêm trọng, ảnh hưởng chức năng chính
- **Trung bình**: Lỗi ảnh hưởng trải nghiệm người dùng
- **Thấp**: Lỗi nhỏ, không ảnh hưởng chức năng

---

### 4.4.3 Đánh giá tổng thể

| Tiêu chí | Kết quả | Ghi chú |
|----------|---------|---------|
| Số test case đã thực hiện | 46 | Toàn bộ test case |
| Tỷ lệ pass | 100% | Tất cả test đều pass |
| Số lỗi phát hiện | 0 | Không có lỗi |
| Chức năng chính | Hoạt động tốt | Đáp ứng yêu cầu |
| Giao diện người dùng | Thân thiện | Dễ sử dụng |
| Bảo mật | Đạt yêu cầu | Phân quyền đúng |
| Hiệu suất | Tốt | Load nhanh, mượt |

**Kết luận:** Hệ thống TechShop Webstore đã hoàn thành toàn bộ test case và đạt yêu cầu về chức năng, giao diện và bảo mật. Hệ thống sẵn sàng để triển khai.
