// Öffentliche Schlüssel für signierte Updates (DEC-356, seit Desktop 4.1.0).
//
// Geschrieben von SFLIX_Tools\Release\update_key.ps1 am 22.11.2026: Hauptschlüssel, Reserveschlüssel.
// Nur die öffentlichen Teile; die privaten Schlüssel liegen in SFLIX_Tools\Release\signing\ (nie pushen) bzw. offline.

namespace Sflix
{
    static class UpdateKeys
    {
        public static readonly string[] PublicKeys = {
            "<RSAKeyValue><Modulus>sI4AbnpKdTon79sr1ITLQzXJpUaine4KmONiIQBhczBlxayptROm1et8M65AEZaV+IfIkRwD7bC4aZzeV1osW+rlss7l33I+nEl31pBKpkQyLOK6VPzlODTYEMJ+0bHcmKqyWLn4h9tWBTsxkl1lBR8l7jb3u7gCrZHfJpk1OIzBikjk6rJIS4iykn7UTuWjlakilBbxCK5L0ep6nsGlskozDSl4fl3q2IgwoBle8UUws1FJOjrvrjtNrEueEr85WDl3fzbMUxMVKAaHH/hpqeir42h9pHMjD2oK5TiRMkScdFN2o7V2zIzhEC5VJ9xyddETgJOoz8eQR3WwuvyG9CLqqjRrW+GLYfGZ7iuu0XSVopvS96BOmykCD3idkML/vn35tBxm59e/7b/E2rDOls3ZaESHqFmnK1N9gRq5HWwlcwVGSvDL3C7JDb1JDGXFgRVMmRxR7RCi0flLzIg3knkUkpV2dTFhDwx1piBXovpxZUKJIHJkmkv7ec8J+Kzh</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
            "<RSAKeyValue><Modulus>lF86muOCQkyCdmwYliovJMM+nx48GYtzOyYUF2kHErO/WA53ajugiPNqacq0ZXDJfTO5ARrM689zbJRttl9Gh/rhq9uXzUj4zJ69L8qRquvbqbfNNDIeARnsYWl3A1a6A532x9LDPWlWKykqCjYzIjX//TkBwi+jnTQELWyDCPMn3cqe/uMgaV3ob5zlwRkej8QIKh9MrmlEHZX1nH0b3FB/UF6jJeI/c5btukIEGeqljrkv5hjMRx+BcX/GzkJumsijCV6wDufKwH3z3NrcKKxdh1QmB5PUnKG//Zk+azTsrzzzzqE62sf+9KfCLJylmBddZ8s1Ae6FeJ2cfEk+E3DzA8stTbLrmchN5phB0XQCliJAZHh5eEoqa797ncgbmE2jlB0Plyyn1GzXZGqMKAQfNy3FaSkWiP5Tnugn8vOGQGqKNfvOntdaEJtaiwPU8baZbdwyImjVGp96e5x5bvUuBL2Aky+KD+dN54WnwQhFVqmOPlrMjeJBt+pK4R5l</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
        };
    }
}
