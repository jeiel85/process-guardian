using System;
using System.IO;
using Xunit;

namespace ProcessGuardian.Tests
{
    public class SlotDataSerializationTests
    {
        [Fact]
        public void SlotData_CanBeSerialized()
        {
            // Arrange
            var slot = new SlotData 
            { 
                Path = "C:\\Test\\app.exe",
                Arguments = "--test",
                MemoryThresholdMB = 2048,
                CpuThresholdPercent = 80,
                MaxRestartCount = 3
            };
            
            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(slot);
            
            // Assert
            Assert.NotNull(json);
            Assert.Contains("app.exe", json);
        }
        
        [Fact]
        public void SlotData_CanBeDeserialized()
        {
            // Arrange
            var json = @"{
                ""Path"": ""C:\\Test\\app.exe"",
                ""Arguments"": ""--test"",
                ""MemoryThresholdMB"": 4096,
                ""CpuThresholdPercent"": 90,
                ""MaxRestartCount"": 5
            }";
            
            // Act
            var slot = System.Text.Json.JsonSerializer.Deserialize<SlotData>(json);
            
            // Assert
            Assert.NotNull(slot);
            Assert.Equal("C:\\Test\\app.exe", slot.Path);
            Assert.Equal("--test", slot.Arguments);
            Assert.Equal(4096, slot.MemoryThresholdMB);
            Assert.Equal(90, slot.CpuThresholdPercent);
            Assert.Equal(5, slot.MaxRestartCount);
        }
        
        [Fact]
        public void SlotData_DefaultValues()
        {
            // Arrange & Act
            var slot = new SlotData();
            
            // Assert
            Assert.Equal(2048, slot.MemoryThresholdMB);
            Assert.Equal(80, slot.CpuThresholdPercent);
            Assert.Equal(3, slot.MaxRestartCount);
        }
    }
    
    public class SlotData
    {
        public string? Path { get; set; }
        public string? Arguments { get; set; }
        public int MemoryThresholdMB { get; set; } = 2048;
        public int CpuThresholdPercent { get; set; } = 80;
        public int MaxRestartCount { get; set; } = 3;
        public string? WebhookUrl { get; set; }
        public bool NotificationsEnabled { get; set; } = false;
        public string? ProfileName { get; set; }
    }
}