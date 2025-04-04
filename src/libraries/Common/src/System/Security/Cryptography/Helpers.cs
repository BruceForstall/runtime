// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
#if NETFRAMEWORK || (NETSTANDARD && !NETSTANDARD2_1_OR_GREATER)
        private static readonly RandomNumberGenerator s_rng = RandomNumberGenerator.Create();
#endif

        [UnsupportedOSPlatformGuard("browser")]
        [UnsupportedOSPlatformGuard("wasi")]
        internal static bool HasSymmetricEncryption { get; } =
#if NET
            !OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi();
#else
            true;
#endif

#if NET
        [UnsupportedOSPlatformGuard("ios")]
        [UnsupportedOSPlatformGuard("tvos")]
        public static bool IsDSASupported => !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS();
#else
        public static bool IsDSASupported => true;
#endif

#if NET
        [UnsupportedOSPlatformGuard("android")]
        [UnsupportedOSPlatformGuard("browser")]
        [UnsupportedOSPlatformGuard("wasi")]
        public static bool IsRC2Supported => !OperatingSystem.IsAndroid() && !OperatingSystem.IsBrowser();
#else
        public static bool IsRC2Supported => true;
#endif

        [UnsupportedOSPlatformGuard("browser")]
        [UnsupportedOSPlatformGuard("wasi")]
        internal static bool HasMD5 { get; } =
#if NET
            !OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi();
#else
            true;
#endif

        [return: NotNullIfNotNull(nameof(src))]
        public static byte[]? CloneByteArray(this byte[]? src)
        {
            return src switch
            {
                null => null,
                { Length: 0 } => src,
                _ => (byte[])src.Clone(),
            };
        }

        internal static bool ContainsNull<T>(this ReadOnlySpan<T> span)
        {
            return Unsafe.IsNullRef(ref MemoryMarshal.GetReference(span));
        }

#if NETFRAMEWORK || (NETSTANDARD && !NETSTANDARD2_1_OR_GREATER)
        internal static void RngFill(byte[] destination)
        {
            s_rng.GetBytes(destination);
        }
#endif

        internal static void RngFill(Span<byte> destination)
        {
#if NET || NETSTANDARD2_1_OR_GREATER
            RandomNumberGenerator.Fill(destination);
#else
            byte[] temp = CryptoPool.Rent(destination.Length);
            s_rng.GetBytes(temp, 0, destination.Length);
            temp.AsSpan(0, destination.Length).CopyTo(destination);
            CryptoPool.Return(temp, destination.Length);
#endif
        }

        internal static bool TryCopyToDestination(this ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (source.TryCopyTo(destination))
            {
                bytesWritten = source.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        internal static int HashOidToByteLength(string hashOid)
        {
            // This file is compiled in netstandard2.0, can't use the HashSizeInBytes consts.
            return hashOid switch
            {
                Oids.Sha256 => 256 >> 3,
                Oids.Sha384 => 384 >> 3,
                Oids.Sha512 => 512 >> 3,
                Oids.Sha1 => 160 >> 3,
                Oids.Md5 => 128 >> 3,
                _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashOid)),
            };
        }

        internal static CryptographicException CreateAlgorithmUnknownException(AsnWriter encodedId)
        {
#if NET10_0_OR_GREATER
            return encodedId.Encode(static encoded =>
                new CryptographicException(
                    SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, Convert.ToHexString(encoded))));
#else
            return new CryptographicException(
                SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier,
                HexConverter.ToString(encodedId.Encode(), HexConverter.Casing.Upper)));
#endif
        }
    }
}
