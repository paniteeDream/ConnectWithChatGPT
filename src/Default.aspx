<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" Async="true" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="/wwwroot/css/index.css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/normalize/8.0.1/normalize.min.css" />

    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <header>
            <nav>
                <img src="/wwwroot/img/logo.png" />
            </nav>
        </header>

        <div class="p center-container">
            <p class="text-center">This is the translation by ChatGPT </p>
            <p class="text-center">
                from <span class="blue-color">Chinese
                </span>to <span class="blue-color">English </span>and <span class="blue-color">Thai</span>
            </p>
        </div>

        <main class="main">
            <section>
                <label for="fileInput" class="ball" id="csvDropLink" runat="server" onclick="">
                    Drop CSV file
                </label>
                <div class="span-wrapper">
                    <span class="s1"></span>
                    <span class="s2"></span>
                    <span class="s3"></span>
                </div>
                <asp:FileUpload ID="fileInput" runat="server" Style="display: none" />
            </section>
        </main>


        <div class="parent-container">
            <asp:LinkButton ID="LinkButton1" runat="server" Text="Translate" OnClick="Translate_Click" OnClientClick="showLoading()" />
            <asp:LinkButton ID="LinkButton2" runat="server" Text="Download" OnClick="Download_Click" />
        </div>
        <div class="parent-container">
        </div>

        <div class="label">
            <asp:Label runat="server" ID="resultLabel" Text=""></asp:Label>
        </div>

        <div class="process">
            <asp:ScriptManager runat="server"></asp:ScriptManager>
            <asp:UpdatePanel ID="updatePanel" runat="server" UpdateMode="Conditional">
                <ContentTemplate>
                    <asp:Label ID="lblProcessingTime" ForeColor="DimGray" runat="server" Text=""></asp:Label>

                </ContentTemplate>
            </asp:UpdatePanel>
        </div>

        <div id="loading" style="display: none;">
            <div class="spinner"></div>
            <p id="translationProgress">Translating...</p>
        </div>




        <asp:GridView ID="resultGridView" runat="server" AutoGenerateColumns="false">
            <Columns>
                <asp:TemplateField HeaderStyle-CssClass="header-background">
                    <ItemTemplate>
                        <%# Container.DataItemIndex + 1 %>
                    </ItemTemplate>
                    <HeaderTemplate>
                        No.
                    </HeaderTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="OriginalWords" HeaderText="Original Words" HeaderStyle-CssClass="header-background" />
                <asp:BoundField DataField="English" HeaderText="English Translation" HeaderStyle-CssClass="header-background" />
                <asp:BoundField DataField="Thai" HeaderText="Thai Translation" HeaderStyle-CssClass="header-background" />
            </Columns>
        </asp:GridView>



        <script>
            document.addEventListener('DOMContentLoaded', function () {
                var fileInput = document.getElementById('<%= fileInput.ClientID %>');
                var csvDropLink = document.getElementById('<%= csvDropLink.ClientID %>');

                // เพิ่ม event listener เมื่อมีการเลือกไฟล์
                fileInput.addEventListener('change', function () {
                    var fileName = this.value.split('\\').pop();  // ดึงชื่อไฟล์ออกมา

                    if (fileName) {
                        // ถ้ามีไฟล์เลือก ให้เปลี่ยนสีพื้นหลังเป็นสีฟ้า
                        csvDropLink.style.backgroundColor = '#2C97E1';
                        csvDropLink.style.color = 'white';
                    } else {
                        // ถ้าไม่มีไฟล์เลือก ให้เปลี่ยนสีพื้นหลังเป็นสีดีฟอลต์
                        csvDropLink.style.backgroundColor = 'gray';
                    }

                    csvDropLink.innerHTML = fileName || 'Please select CSV file';
                });
            });

            function showLoading() {
                document.getElementById('loading').style.display = 'flex';
            }

            function hideLoading() {
                document.getElementById('loading').style.display = 'none';
            }

            /*
            function updateProgress(percentage) {
                var progressElement = document.getElementById('translationProgress');
                progressElement.innerHTML = "Translating... " + percentage.toFixed() + "%";
            }
            */


        </script>


    </form>



    <!--
  <footer>
        &copy; 2023
    </footer>

    -->
</body>
</html>


