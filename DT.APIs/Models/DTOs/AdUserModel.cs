using System;



public class ADUserModel
{
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    public string Mobile { get; set; }
    public string Email { get; set; }
    public string ShortEmail { get; set; }
    public string FullEmail { get; set; }
    public string ManagerName { get; set; }
    public string Company { get; set; }
    public string Sector { get; set; }
    public string Department { get; set; }
    public string DepartmentDetails { get; set; }
    public string Title { get; set; }
    public string DescriptionDetails { get; set; }
    public string Site { get; set; }
    public string DistinguishedName { get; set; }
    public int InstanceType { get; set; }
    public DateTime WhenCreated { get; set; }
    public string OtherTelephone { get; set; }
    public string Address { get; set; }
    public string AdditionalAddressDetails { get; set; }
    public string EmployeeNumber { get; set; }
    public string UserType { get; set; }
    public int SAMAccountType { get; set; }
    public string PhotoBase64 { get; set; }
    public byte[] PhotoBinary { get; set; }

    public string Category { get; set; }

}


// Model for AD authentication request
public class ADAuthenticationRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}


public class ADAuthenticationAndSyncRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public int UserId { get; set; }
}

