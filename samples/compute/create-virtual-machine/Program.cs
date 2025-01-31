﻿using Azure.Identity;
using Azue.ResourceManger;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CreateVMSample
{
    public class Program
    {
        protected static string AdminUsername = "adminUser";
        protected static string AdminPassword = "<password>";
         protected static string DummySSH = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQC+wWK73dCr+jgQOAxNsHAnNNNMEMWOHYEccp6wJm2gotpr9katuF/ZAdou5AaW1C61slRkHRkpRRX9FA9CYBiitZgvCCz+3nWNN7l/Up54Zps/pHWGZLHNJZRYyAB6j5yVLMVHIHriY49d/GZTZVNB8GoJv9Gakwc/fuEZYYl4YDFiGMBP///TzlI4jhiJzjKnEvqPFki5p2ZRJqcbCiF4pJrxUQR/RXqVFQdbRLZgYfJ8xGB878RENq3yQ39d8dVOkq4edbkzwcUmwwwkYVPIoDGsYLaRHnG+To7FvMeyO7xDVQkMKzopTQV8AuKpyvpqu0a9pWOMaiCyDytO7GGN you@me.com";


        static async Task Main(string[] args)
        {
            ArmClient armClient = new ArmClient(new DefaultAzureCredential());

            // Create Resource Group
            Console.WriteLine("--------Start create group--------");
            ResourceGroupContainer resourceGroupContainer = armClient.DefaultSubscription.GetResourceGroups();
            Location location = Location.WestUS2;
            string resourceGroupName = "QuickStartRG";
            ResourceGroupData resourceGroupData = new ResourceGroupData(location);
            ResourceGroup resourceGroup = await resourceGroupContainer.CreateOrUpdateAsync(resourceGroupName, resourceGroupData);
            Console.WriteLine("--------Finish create group--------");

            // Create a Virtual Machine
            await Program.CreateVmAsync(resourceGroup, "quickstartvm");

            Console.ReadKey();

            // Delete resource group if necessary
            Console.WriteLine("--------Start delete group--------");
            await resourceGroup.DeleteAsync();
            Console.WriteLine("--------Finish delete group--------");
        }

        public static async Task CreateVmAsync(
            ResourceGroup resourceGroup,
            string vmName)
        {
            // Create AvailabilitySet
            Console.WriteLine("--------Start create AvailabilitySet--------");
            AvailabilitySetData availabilitySetData = new AvailabilitySetData(resourceGroup.Data.Location)
            {
                PlatformUpdateDomainCount = 5,
                PlatformFaultDomainCount = 2,
                Sku = new Azure.ResourceManager.Compute.Models.Sku() { Name = "Aligned" }
            };
            AvailabilitySet availabilitySet = await resourceGroup.GetAvailabilitySets().CreateOrUpdateAsync(vmName + "_aSet", availabilitySetData);

            // Create IP Address
            Console.WriteLine("--------Start create IP Address--------");
            PublicIPAddressData ipAddressData = new PublicIPAddressData()
            {
                PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                Location = resourceGroup.Data.Location,
            };

            PublicIPAddress ipAddress = await resourceGroup.GetPublicIPAddresses().CreateOrUpdateAsync(vmName + "_ip", ipAddressData);

            // Create VNet
            Console.WriteLine("--------Start create VNet--------");
            VirtualNetworkData vnetData = new VirtualNetworkData()
            {
                Location = resourceGroup.Data.Location,
                AddressSpace = new AddressSpace() { AddressPrefixes = { "10.0.0.0/16" } },
                Subnets = 
                {
                    new SubnetData()
                    {
                        Name = "mySubnet",
                        AddressPrefix = "10.0.0.0/24",
                    }
                },
            };
            VirtualNetwork vnet = await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(vmName + "_vent", vnetData);

            // Create Network Interface
            Console.WriteLine("--------Start create Network Interface--------");
            NetworkInterfaceData nicData = new NetworkInterfaceData()
            {
                Location = resourceGroup.Data.Location,
                IpConfigurations = 
                {
                    new NetworkInterfaceIPConfiguration()
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new SubnetData() { Id = vnet.Data.Subnets.First().Id },
                        PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData() { Id = ipAddress.Id }
                    }
                }
            };
            NetworkInterface nic = await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(vmName + "_nic", nicData);

            // Create VM
            Console.WriteLine("--------Start create VM--------");
            VirtualMachineData vmData = new VirtualMachineData(resourceGroup.Data.Location)
            {
                NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile { NetworkInterfaces = { new NetworkInterfaceReference() { Id = nic.Id } } },
                OsProfile = new OSProfile
                {
                    ComputerName = vmName,
                    AdminUsername = AdminUsername,
                    LinuxConfiguration = new LinuxConfiguration
                    {
                        DisablePasswordAuthentication = true,
                        Ssh =
                        {
                            PublicKeys =
                            {
                                new SshPublicKeyInfo
                                {
                                    Path = $"/home/{AdminUsername}/.ssh/authorized_keys",
                                    KeyData = DummySSH
                                }
                            }
                        },
                        ProvisionVMAgent = true 
                    }
                },
                StorageProfile = new StorageProfile()
                {
@@ -137,16 +138,14 @@ static async Task Main(string[] args)
                        Publisher = "Canonical",
                        Sku = "18.04-LTS",
                        Version = "latest"
                    },
                    DataDisks = new List<DataDisk>()
                    }
                },
                HardwareProfile = new HardwareProfile() { VmSize = VirtualMachineSizeTypes.StandardB1Ms },
                AvailabilitySet = new Azure.ResourceManager.Compute.Models.SubResource() { Id = availabilitySet.Id }
            };

            VirtualMachine vm = await resourceGroup.GetVirtualMachines().CreateOrUpdateAsync(vmName, vmData);
            Console.WriteLine("VM ID: " + vm.Id);
            Console.WriteLine("--------Done create VM--------");
        }
    }

        static async Task Main(string[] args)
        {
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            var resourceClient = new ResourcesManagementClient(subscriptionId, new DefaultAzureCredential());

            // Create Resource Group
            Console.WriteLine("--------Start create group--------");
            var resourceGroups = resourceClient.ResourceGroups;
            var location = "westus2";
            var resourceGroupName = "QuickStartRG";
            var resourceGroup = new ResourceGroup(location);
            resourceGroup = await resourceGroups.CreateOrUpdateAsync(resourceGroupName, resourceGroup);
            Console.WriteLine("--------Finish create group--------");

            // Create a Virtual Machine
            await Program.CreateVmAsync(subscriptionId, "QuickStartRG", location, "quickstartvm");

            // Delete resource group if necessary
            //Console.WriteLine("--------Start delete group--------");
            //await (await resourceGroups.StartDeleteAsync(resourceGroupName)).WaitForCompletionAsync();
            //Console.WriteLine("--------Finish delete group--------");
            //Console.ReadKey();
        }

        public static async Task CreateVmAsync(
            string subscriptionId,
            string resourceGroupName,
            string location,
            string vmName)
        {
            var computeClient = new ComputeManagementClient(subscriptionId, new DefaultAzureCredential());
            var networkClient = new NetworkManagementClient(subscriptionId, new DefaultAzureCredential());
            var virtualNetworksClient = networkClient.VirtualNetworks;
            var networkInterfaceClient = networkClient.NetworkInterfaces;
            var publicIpAddressClient = networkClient.PublicIPAddresses;
            var availabilitySetsClient = computeClient.AvailabilitySets;
            var virtualMachinesClient = computeClient.VirtualMachines;

            // Create AvailabilitySet
            Console.WriteLine("--------Start create AvailabilitySet--------");
            var availabilitySet = new AvailabilitySet(location)
            {
                PlatformUpdateDomainCount = 5,
                PlatformFaultDomainCount = 2,
                Sku = new Azure.ResourceManager.Compute.Models.Sku() { Name = "Aligned" }
            };
            availabilitySet = await availabilitySetsClient.CreateOrUpdateAsync(resourceGroupName, vmName + "_aSet", availabilitySet);

            // Create IP Address
            Console.WriteLine("--------Start create IP Address--------");
            var ipAddress = new PublicIPAddress()
            {
                PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                Location = location,
            };

            ipAddress = await publicIpAddressClient.StartCreateOrUpdate(resourceGroupName, vmName + "_ip", ipAddress)
                .WaitForCompletionAsync();

            // Create VNet
            Console.WriteLine("--------Start create VNet--------");
            var vnet = new VirtualNetwork()
            {
                Location = location,
                AddressSpace = new AddressSpace() { AddressPrefixes = new List<string>() { "10.0.0.0/16" } },
                Subnets = new List<Subnet>()
                {
                    new Subnet()
                    {
                        Name = "mySubnet",
                        AddressPrefix = "10.0.0.0/24",
                    }
                },
            };
            vnet = await virtualNetworksClient
                .StartCreateOrUpdate(resourceGroupName, vmName + "_vent", vnet)
                .WaitForCompletionAsync();

            // Create Network Interface
            Console.WriteLine("--------Start create Network Interface--------");
            var nic = new NetworkInterface()
            {
                Location = location,
                IpConfigurations = new List<NetworkInterfaceIPConfiguration>()
                {
                    new NetworkInterfaceIPConfiguration()
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new Subnet() { Id = vnet.Subnets.First().Id },
                        PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddress() { Id = ipAddress.Id }
                    }
                }
            };
            nic = await networkInterfaceClient
                .StartCreateOrUpdate(resourceGroupName, vmName + "_nic", nic)
                .WaitForCompletionAsync();

            // Create VM
            Console.WriteLine("--------Start create VM--------");
            var vm = new VirtualMachine(location)
            {
                NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile { NetworkInterfaces = new[] { new NetworkInterfaceReference() { Id = nic.Id } } },
                OsProfile = new OSProfile
                {
                    ComputerName = vmName,
                    AdminUsername = Program.AdminUsername,
                    AdminPassword = Program.AdminPassword,
                    LinuxConfiguration = new LinuxConfiguration { DisablePasswordAuthentication = false, ProvisionVMAgent = true }
                },
                StorageProfile = new StorageProfile()
                {
                    ImageReference = new ImageReference()
                    {
                        Offer = "UbuntuServer",
                        Publisher = "Canonical",
                        Sku = "18.04-LTS",
                        Version = "latest"
                    },
                    DataDisks = new List<DataDisk>()
                },
                HardwareProfile = new HardwareProfile() { VmSize = VirtualMachineSizeTypes.StandardB1Ms },
                AvailabilitySet = new Azure.ResourceManager.Compute.Models.SubResource() { Id = availabilitySet.Id }
            };

            var operation = await virtualMachinesClient.StartCreateOrUpdateAsync(resourceGroupName, vmName, vm);
            var result = (await operation.WaitForCompletionAsync()).Value;
            Console.WriteLine("VM ID: " + result.Id);
            Console.WriteLine("--------Done create VM--------");
        }
    }
}
