# HyperV Driver Thread Detection PoC

This VM detection trick utilises thread information from `NtQuerySystemInformation` in order to stealthily detect HyperV's vmbus driver.

This trick can be utilised to stealthily identify any driver that spawns multiple threads on the system, without querying suspicious objects such as driver objects, device objects, driver files, registry keys, or system module information. From an application behaviour perspective, the operation is functionally indistinguishable from enumerating processes.

A full writeup can be found on [Nettitude Labs](https://labs.nettitude.com/blog/).