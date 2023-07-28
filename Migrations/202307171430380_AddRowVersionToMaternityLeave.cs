namespace Leave_Management.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRowVersionToMaternityLeave : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.tblAdmin",
                c => new
                    {
                        Email = c.String(nullable: false, maxLength: 128),
                        Password = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Email);
            
            CreateTable(
                "dbo.tblCasualLeave",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Email = c.String(),
                        Date = c.DateTime(nullable: false),
                        Reason = c.String(),
                        Remarks = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.tblDutyleave",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Email = c.String(),
                        FromDate = c.DateTime(nullable: false),
                        ToDate = c.DateTime(nullable: false),
                        Reason = c.String(),
                        Remarks = c.String(),
                        DutyCertificate = c.String(),
                        Status = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.tblEmployee",
                c => new
                    {
                        Email = c.String(nullable: false, maxLength: 128),
                        Phone = c.String(nullable: false),
                        FullName = c.String(nullable: false),
                        Category = c.String(nullable: false),
                        JoiningDate = c.DateTime(nullable: false),
                        Address = c.String(nullable: false),
                        DateofBirth = c.DateTime(nullable: false),
                        Password = c.String(nullable: false),
                        Status = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Email);
            
            CreateTable(
                "dbo.tblLeaveRequest",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Email = c.String(),
                        Type = c.String(),
                        FromDate = c.DateTime(nullable: false),
                        ToDate = c.DateTime(nullable: false),
                        Reason = c.String(),
                        Remarks = c.String(nullable: false),
                        Status = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.tblLeaveType",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Type = c.String(nullable: false),
                        MaxDayTS = c.Int(nullable: false),
                        MaxDayNTS = c.Int(nullable: false),
                        Description = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.tblMaternityLeave",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Email = c.String(),
                        FromDate = c.DateTime(nullable: false),
                        ToDate = c.DateTime(nullable: false),
                        Remarks = c.String(),
                        Status = c.String(),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.tblMaternityLeave");
            DropTable("dbo.tblLeaveType");
            DropTable("dbo.tblLeaveRequest");
            DropTable("dbo.tblEmployee");
            DropTable("dbo.tblDutyleave");
            DropTable("dbo.tblCasualLeave");
            DropTable("dbo.tblAdmin");
        }
    }
}
