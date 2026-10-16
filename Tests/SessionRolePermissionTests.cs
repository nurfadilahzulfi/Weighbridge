using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    /// <summary>
    /// Role dari @SOL_WB_USER.U_SOL_ROLE: hanya "SU" Super User; "ADMIN" = akun operator timbangan;
    /// role lain/kosong diperlakukan sebagai operator (hak akses terendah). Lihat FormSync.ApplyMenuPermissions.
    /// </summary>
    [TestClass]
    public class SessionRolePermissionTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            SessionUser.username = null;
            SessionUser.userRole = null;
        }

        [TestMethod]
        public void SessionUser_StoreUsernameAndRole_ValuesPersistCorrectly()
        {
            SessionUser.username = "operator1";
            SessionUser.userRole = "ADMIN";

            Assert.AreEqual("operator1", SessionUser.username);
            Assert.AreEqual("ADMIN", SessionUser.userRole);
        }

        [TestMethod]
        public void RolePermissions_SuperUser_CanAccessConfiguration()
        {
            Assert.IsTrue(UserRoles.CanAccessConfiguration("SU"), "SU must have access to Konfigurasi menu.");
        }

        [TestMethod]
        public void RolePermissions_Admin_IsRestrictedFromConfiguration()
        {
            Assert.IsFalse(UserRoles.CanAccessConfiguration("ADMIN"), "ADMIN (operator) must NOT have access to Konfigurasi menu.");
        }

        [TestMethod]
        public void RolePermissions_UnknownOrEmptyRole_DefaultsToRestricted()
        {
            Assert.IsFalse(UserRoles.CanAccessConfiguration(""), "Empty role must not have access to Konfigurasi.");
            Assert.IsFalse(UserRoles.CanAccessConfiguration(null), "Missing role must not have access to Konfigurasi.");
            Assert.IsFalse(UserRoles.CanAccessConfiguration("OPERATOR"), "Unknown role must not have access to Konfigurasi.");
        }

        [TestMethod]
        public void RolePermissions_CaseInsensitiveEvaluation_HandlesProperly()
        {
            Assert.IsTrue(UserRoles.CanAccessConfiguration("su"));
            Assert.IsTrue(UserRoles.CanAccessConfiguration(" SU "));
            Assert.IsFalse(UserRoles.CanAccessConfiguration("admin"));
        }

        [TestMethod]
        public void RoleDisplayName_AdminDanRoleLain_TampilSebagaiOperator()
        {
            Assert.AreEqual("Super User", UserRoles.DisplayName("SU"));
            Assert.AreEqual("Operator", UserRoles.DisplayName("ADMIN"));
            Assert.AreEqual("Operator", UserRoles.DisplayName(""));
            Assert.AreEqual("Operator", UserRoles.DisplayName(null));
        }
    }
}
